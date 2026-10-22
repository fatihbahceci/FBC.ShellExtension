using System.Security.Cryptography;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>Encrypting with a password (ADR-0032), on real files; few iterations, the format records them.</summary>
[TestClass]
public sealed class FileCryptoTests
{
    private const int Chunk = 1024 * 1024;
    private const int FewIterations = 1_000;

    private string _folder = "";

    [TestInitialize]
    public void CreateFolder() => _folder = Directory.CreateTempSubdirectory("fbctools-crypto-").FullName;

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    private string Write(string name, byte[] content)
    {
        string path = Path.Combine(_folder, name);
        System.IO.File.WriteAllBytes(path, content);
        return path;
    }

    private static void Decrypt(string source, string destination, string password) =>
        FileCrypto.Decrypt(source, destination, password, [], CancellationToken.None);

    [TestMethod]
    public void ToolsOfVersion12_OpenTheEditorsWindow()
    {
        var config = new MenuConfig { Version = 11, Items = [new CommandEntry { Label = "Encrypt", Tool = BuiltinTool.Encrypt }] };

        Assert.AreEqual(12, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesEditorWindow(BuiltinTool.Decrypt));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.Encrypt, ItemKind.Folder));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.Decrypt, ItemKind.Background));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(Chunk - 1)]
    [DataRow(Chunk)]
    [DataRow(2 * Chunk)]
    [DataRow((2 * Chunk) + 12345)]
    public void EncryptThenDecrypt_GivesTheSameBytes(int size)
    {
        byte[] content = RandomNumberGenerator.GetBytes(size);
        string plain = Write("a.bin", content);
        string encrypted = Path.Combine(_folder, "a.bin.fbcenc");
        string back = Path.Combine(_folder, "back.bin");
        using (FileCrypto.Key key = FileCrypto.DeriveKey("pässword", FewIterations))
        {
            FileCrypto.Encrypt(plain, encrypted, key, CancellationToken.None);
        }

        Assert.IsTrue(FileCrypto.IsEncrypted(encrypted));
        Assert.IsFalse(FileCrypto.IsEncrypted(plain));

        // The same password typed with a separate accent mark is the same password.
        Decrypt(encrypted, back, "pässword");
        CollectionAssert.AreEqual(content, System.IO.File.ReadAllBytes(back));
    }

    [TestMethod]
    public void Decrypt_RefusesAWrongPasswordAndAnyChange_AndLeavesNothing()
    {
        string plain = Write("a.bin", RandomNumberGenerator.GetBytes((2 * Chunk) + 100));
        string encrypted = Path.Combine(_folder, "a.bin.fbcenc");
        using (FileCrypto.Key key = FileCrypto.DeriveKey("secret", FewIterations))
        {
            FileCrypto.Encrypt(plain, encrypted, key, CancellationToken.None);
        }

        byte[] good = System.IO.File.ReadAllBytes(encrypted);
        string back = Path.Combine(_folder, "back.bin");
        void Refused(byte[] file, string password = "secret")
        {
            System.IO.File.WriteAllBytes(encrypted, file);
            Assert.ThrowsExactly<ToolFailedException>(() => Decrypt(encrypted, back, password));
            Assert.IsFalse(System.IO.File.Exists(back));
        }

        Refused(good, "Secret");

        // A changed byte in the second chunk.
        byte[] flipped = [.. good];
        flipped[39 + Chunk + 16 + 5] ^= 1;
        Refused(flipped);

        // Cut after the second chunk: it was not the last one.
        Refused(good[..(39 + (2 * (Chunk + 16)))]);

        // The first two chunks swapped.
        byte[] swapped = [.. good[..39], .. good[(39 + Chunk + 16)..(39 + (2 * (Chunk + 16)))], .. good[39..(39 + Chunk + 16)], .. good[(39 + (2 * (Chunk + 16)))..]];
        Refused(swapped);

        // Fewer iterations in the header: the header is part of every chunk's check.
        byte[] header = [.. good];
        header[8] ^= 1;
        Refused(header);

        // More after the last chunk: an appended copy of it, or a byte.
        Refused([.. good, .. good[^(100 + 16)..]]);
        Refused([.. good, 0]);

        Refused([.. "not ours at all"u8]);
    }

    [TestMethod]
    public void Decrypt_ChecksTheHeaderWithEveryChunk()
    {
        // A file of one chunk reads the same with a larger chunk size: only the header's check refuses it.
        string plain = Write("small.bin", RandomNumberGenerator.GetBytes(100));
        string encrypted = Path.Combine(_folder, "small.bin.fbcenc");
        using (FileCrypto.Key key = FileCrypto.DeriveKey("secret", FewIterations))
        {
            FileCrypto.Encrypt(plain, encrypted, key, CancellationToken.None);
        }

        byte[] file = System.IO.File.ReadAllBytes(encrypted);
        file[30] = 0x20;
        System.IO.File.WriteAllBytes(encrypted, file);

        Assert.ThrowsExactly<ToolFailedException>(() => Decrypt(encrypted, Path.Combine(_folder, "back.bin"), "secret"));
        Assert.AreEqual(0, Directory.GetFiles(_folder, "*.fbctools-tmp").Length);
    }

    [TestMethod]
    public void Encrypt_NeverTouchesAFileThatIsThere()
    {
        string plain = Write("a.txt", [1, 2, 3]);
        string there = Write("there.fbcenc", [9, 9]);
        using FileCrypto.Key key = FileCrypto.DeriveKey("secret", FewIterations);

        Assert.ThrowsExactly<IOException>(() => FileCrypto.Encrypt(plain, there, key, CancellationToken.None));
        CollectionAssert.AreEqual(new byte[] { 9, 9 }, System.IO.File.ReadAllBytes(there));
    }

    [TestMethod]
    public void EncryptFiles_ThenDecryptFiles_PutEachBackUnderItsName()
    {
        string folder = Directory.CreateDirectory(Path.Combine(_folder, "folder")).FullName;
        string a = Write(Path.Combine("folder", "a.txt"), [.. "alpha"u8]);
        string b = Write(Path.Combine("folder", "b.txt"), [.. "beta"u8]);
        System.IO.File.SetAttributes(b, FileAttributes.ReadOnly);
        // Hidden on both systems: a dot name for Linux, the attribute for Windows (G-0113).
        string hidden = Write(Path.Combine("folder", ".hidden.ini"), [.. "x"u8]);
        System.IO.File.SetAttributes(hidden, FileAttributes.Hidden | FileAttributes.System);

        IReadOnlyList<string> sources = FileTools.CryptSources([Folder(folder)], out List<string> errors, CancellationToken.None);
        Assert.AreEqual(0, errors.Count);
        CollectionAssert.AreEqual(new[] { a, b }, sources.ToArray());

        using (FileCrypto.Key key = FileCrypto.DeriveKey("secret", FewIterations))
        {
            Assert.AreEqual(0, FileTools.EncryptFiles(sources, key, deleteOriginals: true, CancellationToken.None).Count);
            Assert.AreEqual(2, FileTools.EncryptFiles([a + ".fbcenc", b + ".fbcenc"], key, deleteOriginals: false, CancellationToken.None).Count);
        }

        Assert.IsFalse(System.IO.File.Exists(a));
        Assert.IsFalse(System.IO.File.Exists(b));

        // A file of the name is back meanwhile: the decrypted one is numbered, nothing is lost.
        Write(Path.Combine("folder", "a.txt"), [.. "new"u8]);
        Assert.AreEqual(1, FileTools.DecryptFiles([a + ".fbcenc"], "wrong", deleteEncrypted: true, CancellationToken.None).Count);
        Assert.AreEqual(0, FileTools.DecryptFiles([a + ".fbcenc", b + ".fbcenc"], "secret", deleteEncrypted: true, CancellationToken.None).Count);

        Assert.AreEqual("alpha", System.IO.File.ReadAllText(Path.Combine(folder, "a (2).txt")));
        Assert.AreEqual("new", System.IO.File.ReadAllText(a));
        Assert.AreEqual("beta", System.IO.File.ReadAllText(b));
        Assert.IsFalse(System.IO.File.Exists(a + ".fbcenc"));
    }

    [TestMethod]
    public void EncryptFiles_NumbersBeforeTheOriginalExtension()
    {
        string a = Write("a.txt", [.. "one"u8]);
        Write("a.txt.fbcenc", [.. "taken"u8]);
        using FileCrypto.Key key = FileCrypto.DeriveKey("secret", FewIterations);

        Assert.AreEqual(0, FileTools.EncryptFiles([a], key, deleteOriginals: false, CancellationToken.None).Count);

        // Decrypted, it is "a (2).txt", still a text file.
        Assert.IsTrue(System.IO.File.Exists(Path.Combine(_folder, "a (2).txt.fbcenc")));
    }

    [TestMethod]
    public void Encrypt_StopsWhenCancelled_AndLeavesNothing()
    {
        string plain = Write("a.bin", new byte[3 * Chunk]);
        string encrypted = Path.Combine(_folder, "a.bin.fbcenc");
        using FileCrypto.Key key = FileCrypto.DeriveKey("secret", FewIterations);

        Assert.ThrowsExactly<OperationCanceledException>(() => FileCrypto.Encrypt(plain, encrypted, key, new CancellationToken(canceled: true)));
        Assert.IsFalse(System.IO.File.Exists(encrypted));

        FileCrypto.Encrypt(plain, encrypted, key, CancellationToken.None);
        string back = Path.Combine(_folder, "back.bin");
        Assert.ThrowsExactly<OperationCanceledException>(() => FileCrypto.Decrypt(encrypted, back, "secret", [], new CancellationToken(canceled: true)));
        Assert.IsFalse(System.IO.File.Exists(back));
        Assert.AreEqual(0, Directory.GetFiles(_folder, "*.fbctools-tmp").Length);
    }
}
