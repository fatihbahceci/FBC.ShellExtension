using System.Text.Json;
using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.WikiImport;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Rename from IMDb's settings, in <c>%APPDATA%\FBCTools\omdb.json</c> next to the menu's config
/// (ADR-0046): the OMDb API key, encrypted for the Windows user with the same DPAPI protector as
/// Send to Wiki.js's, and the last name template.
/// </summary>
internal sealed class OmdbSettings
{
    public const string FileName = "omdb.json";

    /// <summary>Encrypted; see <see cref="DpapiProtector"/>.</summary>
    public string? ApiKey { get; set; }

    public string? Format { get; set; }

    public static string DefaultPath => Path.Combine(ConfigLocation.FolderPath, FileName);

    /// <summary>The key, decrypted; null when none is kept or it cannot be decrypted (another user's, another machine's).</summary>
    public string? Key(DpapiProtector protector) => ApiKey is { } stored ? protector.Unprotect(stored) : null;

    public void SetKey(string? key, DpapiProtector protector) => ApiKey = key is null ? null : protector.Protect(key);

    /// <summary>A missing file gives the defaults.</summary>
    /// <exception cref="IOException">The file cannot be read; it is left as it is.</exception>
    public static OmdbSettings Load(string path)
    {
        try
        {
            return File.Exists(path) ? FromJson(File.ReadAllText(path), path) : new OmdbSettings();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || (ex is IOException && ex.InnerException is not JsonException))
        {
            throw new IOException(Strings.OmdbSettingsUnreadable(path, ex.Message), ex);
        }
    }

    /// <summary>The settings in <paramref name="text"/>: a file's, or a backup's (ADR-0048).</summary>
    /// <param name="source">Where the text came from, for the message.</param>
    /// <exception cref="IOException">The text is not settings.</exception>
    public static OmdbSettings FromJson(string text, string source)
    {
        try
        {
            return JsonSerializer.Deserialize(text, OmdbSettingsContext.Default.OmdbSettings) ?? new OmdbSettings();
        }
        catch (JsonException ex)
        {
            throw new IOException(Strings.OmdbSettingsUnreadable(source, ex.Message), ex);
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, OmdbSettingsContext.Default.OmdbSettings);

    /// <summary>Reads the file again, makes <paramref name="change"/> and saves it: the key window and a rename window may both write.</summary>
    /// <exception cref="IOException">The file cannot be read or written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be written.</exception>
    public static void Update(string path, Action<OmdbSettings> change)
    {
        OmdbSettings settings = Load(path);
        change(settings);
        settings.Save(path);
    }

    /// <summary>Whole to a temporary file of its own first: a failure never leaves half a file.</summary>
    /// <exception cref="IOException">The file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused.</exception>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, ToJson());
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(OmdbSettings))]
internal sealed partial class OmdbSettingsContext : JsonSerializerContext
{
}
