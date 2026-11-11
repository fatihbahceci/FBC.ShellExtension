using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Core.Setup;

/// <summary>
/// The keys a backup carries, as they are and so readable on any computer: the OMDb API key and
/// each Wiki.js site's key or token, by the site's address (ADR-0048). In the settings files
/// they are encrypted for one Windows user (DPAPI), which no other computer can read.
/// </summary>
public sealed class BackupKeys
{
    public string? Omdb { get; set; }

    public Dictionary<string, string> Wiki { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public int Count => (Omdb is null ? 0 : 1) + Wiki.Count;
}

/// <summary>
/// What a backup holds: the menu, the settings files of Rename from IMDb and Send to Wiki.js
/// without their keys, and the keys: as they are (<see cref="PlainKeys"/>), encrypted with the
/// backup's password (<see cref="EncryptedKeys"/>), or none.
/// </summary>
public sealed record BackupContent(string Config, string? OmdbSettings, string? WikiSettings, BackupKeys? PlainKeys, byte[]? EncryptedKeys);

/// <summary>The file is not an FBCTools backup, or not a whole one.</summary>
public sealed class BackupException(string message) : Exception(message);

/// <summary>
/// The backup: one <c>.zip</c> with the menu, the tools' settings and their keys (ADR-0048).
/// The keys are in an entry of their own: <c>keys.fbcenc</c> in <see cref="FileCrypto"/>'s
/// format when a password protects them, <c>keys.json</c> when the user chose plain text.
/// </summary>
public static class BackupArchive
{
    public const string Extension = ".zip";

    private const int Format = 1;
    private const string ManifestEntry = "fbctools-backup.json";
    private const string ConfigEntry = "config.json";
    private const string OmdbEntry = "omdb.json";
    private const string WikiEntry = "wiki-import.json";
    private const string PlainKeysEntry = "keys.json";
    private const string EncryptedKeysEntry = "keys.fbcenc";

    // Far above any menu with icons; an entry that is more is not read into memory.
    private const long MaxEntryBytes = 64L * 1024 * 1024;

    /// <summary>Writes the backup whole, or leaves whatever was at <paramref name="path"/> as it was.</summary>
    /// <param name="keys">Left out when null or empty.</param>
    /// <param name="password">Encrypts the keys; null writes them as they are.</param>
    /// <exception cref="IOException">Writing failed.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused.</exception>
    public static void Write(string path, string config, string? omdbSettings, string? wikiSettings, BackupKeys? keys, FileCrypto.Key? password)
    {
        string full = Path.GetFullPath(path);
        string temporary = $"{full}.{Guid.NewGuid():N}.tmp";
        bool withKeys = keys is { Count: > 0 };
        try
        {
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                var manifest = new BackupManifest
                {
                    Format = Format,
                    Version = ProductInfo.Version.ToString(),
                    Keys = !withKeys ? "none" : password is null ? "plain" : "encrypted",
                };
                Add(archive, ManifestEntry, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, BackupJsonContext.Default.BackupManifest)));
                Add(archive, ConfigEntry, Encoding.UTF8.GetBytes(config));
                if (omdbSettings is not null)
                {
                    Add(archive, OmdbEntry, Encoding.UTF8.GetBytes(omdbSettings));
                }

                if (wikiSettings is not null)
                {
                    Add(archive, WikiEntry, Encoding.UTF8.GetBytes(wikiSettings));
                }

                if (withKeys)
                {
                    byte[] plain = JsonSerializer.SerializeToUtf8Bytes(keys!, BackupJsonContext.Default.BackupKeys);
                    try
                    {
                        Add(archive, password is null ? PlainKeysEntry : EncryptedKeysEntry, password is null ? plain : FileCrypto.Encrypt(plain, password));
                    }
                    finally
                    {
                        Array.Clear(plain);
                    }
                }
            }

            File.Move(temporary, full, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    /// <summary>Whether the file starts as a zip does; a backup is, an old one-file backup (<c>.json</c>) is not.</summary>
    public static bool IsArchive(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Span<byte> start = stackalloc byte[4];
            return stream.ReadAtLeast(start, start.Length, throwOnEndOfStream: false) == start.Length
                && start.SequenceEqual("PK\x03\x04"u8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <exception cref="BackupException">Not a backup of ours, one of a newer format, or one without its menu.</exception>
    /// <exception cref="IOException">Reading failed.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused.</exception>
    public static BackupContent Read(string path)
    {
        try
        {
            using ZipArchive archive = ZipFile.OpenRead(path);
            BackupManifest? manifest = Entry(archive, ManifestEntry) is { } text
                ? JsonSerializer.Deserialize(text, BackupJsonContext.Default.BackupManifest)
                : null;
            if (manifest is null)
            {
                throw new BackupException(Strings.BackupNotOurs(path));
            }

            if (manifest.Format > Format)
            {
                throw new BackupException(Strings.BackupNewer(path, manifest.Version ?? "?"));
            }

            string config = Text(Entry(archive, ConfigEntry)) ?? throw new BackupException(Strings.BackupNotOurs(path));
            BackupKeys? plainKeys = Text(Entry(archive, PlainKeysEntry)) is { } keysText
                ? JsonSerializer.Deserialize(keysText, BackupJsonContext.Default.BackupKeys)
                : null;
            return new BackupContent(config, Text(Entry(archive, OmdbEntry)), Text(Entry(archive, WikiEntry)), Clean(plainKeys),
                Entry(archive, EncryptedKeysEntry));
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            throw new BackupException(Strings.BackupNotOurs(path));
        }
    }

    /// <summary>The keys <see cref="Write"/> encrypted with <paramref name="password"/>.</summary>
    /// <exception cref="ToolFailedException">A wrong password, or a changed file.</exception>
    public static BackupKeys DecryptKeys(byte[] encrypted, string password)
    {
        byte[] plain = FileCrypto.Decrypt(encrypted, password);
        try
        {
            return Clean(JsonSerializer.Deserialize(plain, BackupJsonContext.Default.BackupKeys))
                ?? throw new ToolFailedException(Strings.CryptWrongPassword());
        }
        catch (JsonException)
        {
            throw new ToolFailedException(Strings.CryptWrongPassword());
        }
        finally
        {
            Array.Clear(plain);
        }
    }

    /// <summary>A hand-written <c>null</c> map reads as an empty one; empty keys are no keys.</summary>
    private static BackupKeys? Clean(BackupKeys? keys)
    {
        if (keys is null)
        {
            return null;
        }

        var wiki = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string site, string key) in keys.Wiki ?? [])
        {
            if (!string.IsNullOrWhiteSpace(site) && !string.IsNullOrWhiteSpace(key))
            {
                wiki[site] = key;
            }
        }

        return new BackupKeys { Omdb = string.IsNullOrWhiteSpace(keys.Omdb) ? null : keys.Omdb, Wiki = wiki };
    }

    private static void Add(ZipArchive archive, string name, byte[] bytes)
    {
        using Stream stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
        stream.Write(bytes);
    }

    private static byte[]? Entry(ZipArchive archive, string name)
    {
        if (archive.GetEntry(name) is not { } entry)
        {
            return null;
        }

        // The size the zip declares is not trusted: a Zip64 field can say -1, and a stored entry
        // is read to its compressed length whatever it declares. What is read counts.
        if (entry.Length is < 0 or > MaxEntryBytes)
        {
            throw new InvalidDataException();
        }

        using Stream stream = entry.Open();
        using var memory = new MemoryStream();
        byte[] buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer)) > 0)
        {
            if (memory.Length + read > entry.Length)
            {
                throw new InvalidDataException();
            }

            memory.Write(buffer, 0, read);
        }

        return memory.ToArray();
    }

    private static string? Text(byte[]? bytes) => bytes is null ? null : new UTF8Encoding(false).GetString(bytes).TrimStart('﻿');
}

/// <summary>The archive's own entry: which format it is in and how its keys are kept.</summary>
internal sealed class BackupManifest
{
    public int Format { get; set; }

    /// <summary>The FBCTools version that wrote it.</summary>
    public string? Version { get; set; }

    /// <summary><c>encrypted</c>, <c>plain</c> or <c>none</c>.</summary>
    public string? Keys { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(BackupManifest))]
[JsonSerializable(typeof(BackupKeys))]
internal sealed partial class BackupJsonContext : JsonSerializerContext
{
}
