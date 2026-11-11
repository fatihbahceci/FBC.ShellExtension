using System.IO.Compression;
using System.Text;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Setup;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>The backup .zip: the menu, the tools' settings and their keys (ADR-0048).</summary>
[TestClass]
public sealed class BackupArchiveTests
{
    private const string Config = """{ "version": 16, "items": [{ "type": "command", "label": "Copy path", "tool": "copyPath" }] }""";

    private string _folder = "";

    [TestInitialize]
    public void CreateFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fbc-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    private static BackupKeys Keys() => new()
    {
        Omdb = "omdb-key",
        Wiki = new(StringComparer.OrdinalIgnoreCase) { ["https://wiki.example.com"] = "wiki-token" },
    };

    [TestMethod]
    public void Write_EncryptsTheKeysWithThePassword()
    {
        string path = Path.Combine(_folder, "backup.zip");
        using (FileCrypto.Key key = FileCrypto.DeriveKey("correct horse", iterations: 1000))
        {
            BackupArchive.Write(path, Config, """{ "format": "x" }""", """{ "sites": [] }""", Keys(), key);
        }

        Assert.IsTrue(BackupArchive.IsArchive(path));
        BackupContent content = BackupArchive.Read(path);
        Assert.AreEqual(Config, content.Config);
        Assert.AreEqual("""{ "format": "x" }""", content.OmdbSettings);
        Assert.AreEqual("""{ "sites": [] }""", content.WikiSettings);
        Assert.IsNull(content.PlainKeys);
        Assert.IsNotNull(content.EncryptedKeys);

        // Nowhere in the archive as they are.
        using (ZipArchive archive = ZipFile.OpenRead(path))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                using var reader = new StreamReader(entry.Open(), Encoding.Latin1);
                string text = reader.ReadToEnd();
                StringAssert.DoesNotMatch(text, new System.Text.RegularExpressions.Regex("omdb-key|wiki-token"));
            }
        }

        BackupKeys keys = BackupArchive.DecryptKeys(content.EncryptedKeys!, "correct horse");
        Assert.AreEqual("omdb-key", keys.Omdb);
        Assert.AreEqual("wiki-token", keys.Wiki["https://WIKI.example.com"]);
        Assert.ThrowsExactly<ToolFailedException>(() => BackupArchive.DecryptKeys(content.EncryptedKeys!, "wrong"));
    }

    [TestMethod]
    public void Write_KeepsTheKeysAsTheyAreOrLeavesThemOut()
    {
        string plain = Path.Combine(_folder, "plain.zip");
        BackupArchive.Write(plain, Config, null, null, Keys(), null);
        BackupContent withPlain = BackupArchive.Read(plain);
        Assert.AreEqual("omdb-key", withPlain.PlainKeys!.Omdb);
        Assert.AreEqual("wiki-token", withPlain.PlainKeys.Wiki["https://wiki.example.com"]);
        Assert.IsNull(withPlain.EncryptedKeys);
        Assert.IsNull(withPlain.OmdbSettings);

        string none = Path.Combine(_folder, "none.zip");
        BackupArchive.Write(none, Config, null, null, new BackupKeys(), null);
        BackupContent withNone = BackupArchive.Read(none);
        Assert.IsNull(withNone.PlainKeys);
        Assert.IsNull(withNone.EncryptedKeys);

        // A second write replaces the first.
        BackupArchive.Write(none, Config, "{}", null, null, null);
        Assert.AreEqual("{}", BackupArchive.Read(none).OmdbSettings);
        Assert.AreEqual(1, Directory.GetFiles(_folder, "none.zip*").Length);
    }

    [TestMethod]
    public void Read_RefusesWhatIsNotABackup()
    {
        string json = Path.Combine(_folder, "config.json");
        System.IO.File.WriteAllText(json, Config);
        Assert.IsFalse(BackupArchive.IsArchive(json));
        Assert.ThrowsExactly<BackupException>(() => BackupArchive.Read(json));

        Assert.ThrowsExactly<BackupException>(() => BackupArchive.Read(Zip("other.zip", ("readme.txt", "hello"))));
        Assert.ThrowsExactly<BackupException>(() => BackupArchive.Read(Zip("no-menu.zip", ("fbctools-backup.json", """{ "format": 1 }"""))));
        BackupException newer = Assert.ThrowsExactly<BackupException>(() =>
            BackupArchive.Read(Zip("newer.zip", ("fbctools-backup.json", """{ "format": 2, "version": "9.0.0" }"""), ("config.json", Config))));
        StringAssert.Contains(newer.Message, "9.0.0");
    }

    [TestMethod]
    public void Read_ReadsNoMoreThanAnEntryDeclares()
    {
        string path = Path.Combine(_folder, "lying.zip");
        using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("fbctools-backup.json").Open()))
            {
                writer.Write("""{ "format": 1 }""");
            }

            using var stored = new StreamWriter(archive.CreateEntry("config.json", CompressionLevel.NoCompression).Open());
            stored.Write(new string(' ', 1000));
        }

        // config.json says it is 10 bytes, in its own header and in the directory.
        byte[] bytes = System.IO.File.ReadAllBytes(path);
        byte[] name = Encoding.ASCII.GetBytes("config.json");
        int patched = 0;
        for (int at = 0; at + 46 + name.Length <= bytes.Length; at++)
        {
            bool local = bytes.AsSpan(at, 4).SequenceEqual("PK\x03\x04"u8) && bytes.AsSpan(at + 30, name.Length).SequenceEqual(name);
            bool central = bytes.AsSpan(at, 4).SequenceEqual("PK\x01\x02"u8) && bytes.AsSpan(at + 46, name.Length).SequenceEqual(name);
            if (local || central)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(at + (local ? 22 : 24)), 10);
                patched++;
            }
        }

        Assert.AreEqual(2, patched);
        System.IO.File.WriteAllBytes(path, bytes);
        Assert.ThrowsExactly<BackupException>(() => BackupArchive.Read(path));
    }

    private string Zip(string name, params (string Entry, string Text)[] entries)
    {
        string path = Path.Combine(_folder, name);
        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach ((string entry, string text) in entries)
        {
            using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
            writer.Write(text);
        }

        return path;
    }

    [TestMethod]
    public void FileCrypto_EncryptsBytesInTheFilesFormat()
    {
        byte[] plain = Encoding.UTF8.GetBytes("secret");
        byte[] encrypted;
        using (FileCrypto.Key key = FileCrypto.DeriveKey("pw", iterations: 1000))
        {
            encrypted = FileCrypto.Encrypt(plain, key);
        }

        string file = Path.Combine(_folder, "keys" + FileCrypto.Extension);
        System.IO.File.WriteAllBytes(file, encrypted);
        Assert.IsTrue(FileCrypto.IsEncrypted(file));
        CollectionAssert.AreEqual(plain, FileCrypto.Decrypt(encrypted, "pw"));
        Assert.ThrowsExactly<ToolFailedException>(() => FileCrypto.Decrypt(encrypted, "other"));
        Assert.ThrowsExactly<ToolFailedException>(() => FileCrypto.Decrypt([1, 2, 3], "pw"));

        // And a file of them decrypts as any file does.
        string output = Path.Combine(_folder, "keys.txt");
        FileCrypto.Decrypt(file, output, "pw", [], CancellationToken.None);
        Assert.AreEqual("secret", System.IO.File.ReadAllText(output));
    }

    [TestMethod]
    public void RestoreText_PutsTheBackupsMenuInPlace()
    {
        string config = Path.Combine(_folder, "config.json");
        System.IO.File.WriteAllText(config, """{ "version": 16, "items": [] }""");

        string? aside = ConfigMaintenance.RestoreText(config, Config, new DateTime(2026, 11, 10, 12, 0, 0));

        Assert.AreEqual(Config, System.IO.File.ReadAllText(config));
        Assert.IsNotNull(aside);
        Assert.AreEqual("""{ "version": 16, "items": [] }""", System.IO.File.ReadAllText(aside));
        Assert.ThrowsExactly<ConfigLoadException>(() => ConfigMaintenance.RestoreText(config, "{ nope", DateTime.Now));
    }
}
