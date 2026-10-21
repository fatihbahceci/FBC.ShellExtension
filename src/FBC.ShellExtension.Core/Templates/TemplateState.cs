using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Templates;

/// <summary>
/// What the last template update left behind, kept next to the config (ADR-0009): which
/// template entries the user has already been given, and what the config looked like when it
/// was last written by an update.
/// </summary>
public sealed class TemplateState
{
    public const string FileName = "template-state.json";

    /// <summary>SHA-256 of the config text as the last update wrote it; null when the user had changed it.</summary>
    public string? ConfigHash { get; set; }

    /// <summary>SHA-256 of the template text last applied.</summary>
    public string? TemplateHash { get; set; }

    /// <summary>Every template id the user has been given, including those they deleted since.</summary>
    public List<string> AppliedIds { get; set; } = [];

    /// <summary>
    /// The FBCTools version that last applied a template. An older copy leaves the config to a
    /// newer one instead of taking it back to its own, older template.
    /// </summary>
    public string? AppliedBy { get; set; }

    public static string PathFor(string configPath) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configPath))!, FileName);

    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    /// <returns>Null when there is no state or it cannot be read; an update then starts afresh.</returns>
    public static TemplateState? Load(string path)
    {
        try
        {
            TemplateState? state = File.Exists(path) ? JsonSerializer.Deserialize(File.ReadAllText(path), TemplateJsonContext.Default.TemplateState) : null;
            if (state is not null)
            {
                state.AppliedIds = [.. (state.AppliedIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id))];
            }

            return state;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save(string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(this, TemplateJsonContext.Default.TemplateState), new UTF8Encoding(false));
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(TemplateState))]
internal sealed partial class TemplateJsonContext : JsonSerializerContext
{
}
