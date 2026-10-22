using System.Text.Json;
using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.WikiImport;

/// <summary>How an image's link is written into the page.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<LinkStyle>))]
public enum LinkStyle
{
    /// <summary><c>/internal/page/image.png</c>: works under any name the wiki is reached by.</summary>
    Path,

    /// <summary><c>https://wiki.example.com/internal/page/image.png</c>.</summary>
    FullUrl,
}

/// <summary>
/// The import's settings, in <c>%APPDATA%\FBCTools\wiki-import.json</c> next to the menu's
/// config (ADR-0020). The API keys in it are encrypted for the Windows user (DPAPI); a key in
/// the <c>FBC_WIKI_APIKEY</c> environment variable is used instead and never stored.
/// </summary>
public sealed class WikiSettings
{
    public const string FileName = "wiki-import.json";

    public const string ApiKeyVariable = "FBC_WIKI_APIKEY";

    /// <summary>Shorter than this is no Wiki.js API key or token; the runner and the editor refuse it.</summary>
    public const int MinimumKeyLength = 20;

    public LinkStyle LinkStyle { get; set; } = LinkStyle.Path;

    /// <summary>Wiki.js shows the title above the page; the file's own first heading would repeat it.</summary>
    public bool RemoveFirstHeading { get; set; } = true;

    public List<WikiSiteSettings> Sites { get; set; } = [];

    /// <summary>The page each file went to last, offered again for the next import.</summary>
    public List<WikiTarget> Targets { get; set; } = [];

    public static string DefaultPath => Path.Combine(ConfigLocation.FolderPath, FileName);

    public string? TargetFor(string file) =>
        Targets.FirstOrDefault(target => string.Equals(target.File, file, StringComparison.OrdinalIgnoreCase))?.Url;

    public void SetTarget(string file, string url)
    {
        Targets.RemoveAll(target => string.Equals(target.File, file, StringComparison.OrdinalIgnoreCase));
        Targets.Add(new WikiTarget { File = file, Url = url });
    }

    /// <summary>The stored key for the site, decrypted; null when there is none or it cannot be decrypted (another user's, another machine's).</summary>
    public string? KeyFor(string baseUrl, ISecretProtector protector) =>
        Sites.FirstOrDefault(site => string.Equals(site.BaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase))?.ApiKey is { } stored
            ? protector.Unprotect(stored)
            : null;

    public void SetKey(string baseUrl, string? apiKey, ISecretProtector protector)
    {
        Sites.RemoveAll(site => string.Equals(site.BaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase));
        if (apiKey is not null)
        {
            Sites.Add(new WikiSiteSettings { BaseUrl = baseUrl, ApiKey = protector.Protect(apiKey) });
        }
    }

    /// <summary>A missing file gives the defaults; a hand-written <c>null</c> list reads as an empty one.</summary>
    /// <exception cref="WikiException">The file cannot be read; it is left as it is.</exception>
    public static WikiSettings Load(string path)
    {
        WikiSettings? settings;
        try
        {
            settings = File.Exists(path)
                ? JsonSerializer.Deserialize(File.ReadAllText(path), WikiSettingsContext.Default.WikiSettings)
                : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new WikiException(Strings.WikiSettingsInvalid(path, ex.Message));
        }

        settings ??= new WikiSettings();
        settings.Sites = [.. (settings.Sites ?? []).Where(site => site is not null)];
        settings.Targets = [.. (settings.Targets ?? []).Where(target => target is not null)];
        return settings;
    }

    /// <summary>
    /// Reads the file again, makes <paramref name="change"/> to it and saves it, so that two
    /// imports at the same time keep each other's keys and targets.
    /// </summary>
    /// <exception cref="WikiException">The file cannot be read; it is left as it is.</exception>
    /// <exception cref="IOException">The file cannot be written.</exception>
    public static void Update(string path, Action<WikiSettings> change)
    {
        WikiSettings settings = Load(path);
        change(settings);
        settings.Save(path);
    }

    /// <summary>
    /// Written whole to a temporary file of its own first, so a failure never leaves half a file
    /// and two saves at once do not write into the same one.
    /// </summary>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, WikiSettingsContext.Default.WikiSettings));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}

public sealed class WikiSiteSettings
{
    public string BaseUrl { get; set; } = "";

    /// <summary>Encrypted for the Windows user; see <see cref="ISecretProtector"/>.</summary>
    public string? ApiKey { get; set; }
}

public sealed class WikiTarget
{
    public string File { get; set; } = "";

    public string Url { get; set; } = "";
}

/// <summary>Keeps an API key unreadable to anyone but the Windows user: DPAPI in the runner, a fake in the tests.</summary>
public interface ISecretProtector
{
    string Protect(string secret);

    /// <returns>Null when the text cannot be decrypted here.</returns>
    string? Unprotect(string protectedText);
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(WikiSettings))]
internal sealed partial class WikiSettingsContext : JsonSerializerContext
{
}
