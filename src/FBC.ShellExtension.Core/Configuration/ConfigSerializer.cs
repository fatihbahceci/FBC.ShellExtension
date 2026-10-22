using System.Text;
using System.Text.Json;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>Reads and writes <see cref="MenuConfig"/> through the source-generated context only.</summary>
public static class ConfigSerializer
{
    /// <exception cref="ConfigLoadException">The text is not a valid configuration document.</exception>
    public static MenuConfig Deserialize(string json)
    {
        MenuConfig? config;
        try
        {
            config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.MenuConfig);
        }
        // A missing "type" on an entry surfaces as NotSupportedException, not JsonException;
        // its message still names the location.
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // What a newer FBCTools added cannot be read here: that is said, rather than "invalid" (ADR-0038).
            if (DeclaredVersion(json) is int version && version > MenuConfig.CurrentVersion)
            {
                throw new ConfigNewerException(Strings.ConfigNewer(version, MenuConfig.CurrentVersion, ex.Message), version, ex);
            }

            throw new ConfigLoadException(Strings.ConfigInvalidJson(ex.Message), ex);
        }

        return config ?? throw new ConfigLoadException(Strings.ConfigEmpty(), null);
    }

    public static string Serialize(MenuConfig config) =>
        JsonSerializer.Serialize(config, ConfigJsonContext.Default.MenuConfig);

    /// <exception cref="ConfigLoadException">The file is not a valid configuration document.</exception>
    public static MenuConfig Load(string path) => Deserialize(File.ReadAllText(path));

    /// <summary>
    /// Writes the file in one step: the text goes to a temporary file in the same folder, which
    /// then replaces the config, so a right-click in between never reads half a document.
    /// </summary>
    public static void Save(MenuConfig config, string path) => SaveText(Serialize(config) + Environment.NewLine, path);

    /// <summary>The same one-step write for a document that is already text, such as the template with its comments.</summary>
    public static void SaveText(string text, string path)
    {
        string fullPath = Path.GetFullPath(path);
        string folder = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(folder);
        string temporary = Path.Combine(folder, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporary, fullPath, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The original failure is the one to report.
            }

            throw;
        }
    }

    /// <summary>
    /// The "version" the document declares at its top level, read without the rest of it; null
    /// when it declares none, or is not JSON up to there.
    /// </summary>
    public static int? DeclaredVersion(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                bool isVersion = reader.ValueTextEquals("version"u8);
                reader.Read();
                if (isVersion)
                {
                    return reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int version) ? version : null;
                }

                reader.Skip();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    /// <summary>
    /// Whether the text has comments, which <see cref="Serialize"/> cannot write back. False for
    /// text that is not JSON at all.
    /// </summary>
    public static bool ContainsComments(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Allow,
            AllowTrailingCommas = true,
        });

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }
}

public class ConfigLoadException(string message, Exception? innerException)
    : Exception(message, innerException);

/// <summary>
/// The document was written by a newer FBCTools and uses what this one does not know. An older
/// program must not save over it: what it cannot read, it would drop (ADR-0038).
/// </summary>
public sealed class ConfigNewerException(string message, int version, Exception? innerException)
    : ConfigLoadException(message, innerException)
{
    /// <summary>The version the document declares.</summary>
    public int Version { get; } = version;
}
