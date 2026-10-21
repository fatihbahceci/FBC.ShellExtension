using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FBC.ShellExtension.WikiImport;

/// <summary>
/// The names images are uploaded under. Wiki.js lower-cases a name, turns spaces, commas,
/// semicolons and # into "_" and drops what Windows forbids (server/controllers/upload.js); its
/// form parser decodes a non-ASCII name wrongly. So a name is made plain ASCII here, already in
/// the server's form, and the server is then asked what it stored (ADR-0020).
/// </summary>
public static partial class AssetNames
{
    private static readonly string[] s_deviceNames =
    [
        "con", "prn", "aux", "nul",
        "com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    ];

    /// <summary><c>Ekran Görüntüsü 1.PNG</c> → <c>ekran_goruntusu_1.png</c>.</summary>
    public static string ForUpload(string fileName)
    {
        // Letters lose their accents first (ş → s, İ → I), then case: the invariant culture does
        // not lower İ, and a dotless ı has no accent to lose.
        var unaccented = new StringBuilder(fileName.Length);
        foreach (char c in fileName.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                unaccented.Append(c);
            }
        }

        var plain = new StringBuilder(unaccented.Length);
        foreach (char c in unaccented.ToString().ToLowerInvariant().Replace('ı', 'i').Replace("ß", "ss"))
        {
            plain.Append(c is > '\u007f' || char.IsControl(c) || "/?<>\\:*|\"".Contains(c) ? '_' : c);
        }

        // The asset route drops ".." from the address it is asked for, so a name with it is never found.
        string name = Dots().Replace(Separators().Replace(plain.ToString(), "_"), ".").TrimEnd('.', ' ');
        string extension = Path.GetExtension(name);
        string stem = name[..^extension.Length].Trim('_');
        if (stem.Length == 0)
        {
            stem = "image";
        }

        if (s_deviceNames.Contains(stem))
        {
            stem = "_" + stem;
        }

        return stem + extension;
    }

    /// <summary>
    /// An upload name for each file, made unique: two files that come out with the same name
    /// (one from each of two folders, or <c>A.png</c> and <c>a.png</c>) get "-2", "-3" … A file
    /// without an extension gets one from its first bytes: the server serves only a name with
    /// an extension as a file, and takes any other address for a page.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ForUpload(IEnumerable<string> paths)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            if (names.ContainsKey(path))
            {
                continue;
            }

            string name = ForUpload(Path.GetFileName(path));
            if (Path.GetExtension(name).Length == 0)
            {
                name += ExtensionOf(path);
            }

            names[path] = Free(name, taken);
        }

        return names;
    }

    /// <summary><paramref name="name"/>, or with "-2", "-3" … when it is taken; the result is then taken too.</summary>
    public static string Free(string name, ISet<string> taken)
    {
        string extension = Path.GetExtension(name);
        string stem = name[..^extension.Length];
        string free = name;
        for (int number = 2; !taken.Add(free); number++)
        {
            free = string.Create(CultureInfo.InvariantCulture, $"{stem}-{number}{extension}");
        }

        return free;
    }

    /// <summary>The extension an image's first bytes call for; ".bin" for anything else.</summary>
    private static string ExtensionOf(string path)
    {
        Span<byte> head = stackalloc byte[12];
        int read = 0;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            read = stream.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        head = head[..read];
        return head switch
        {
            [0x89, (byte)'P', (byte)'N', (byte)'G', ..] => ".png",
            [0xFF, 0xD8, 0xFF, ..] => ".jpg",
            [(byte)'G', (byte)'I', (byte)'F', (byte)'8', ..] => ".gif",
            [(byte)'R', (byte)'I', (byte)'F', (byte)'F', _, _, _, _, (byte)'W', (byte)'E', (byte)'B', (byte)'P'] => ".webp",
            [(byte)'B', (byte)'M', ..] => ".bmp",
            _ => ".bin",
        };
    }

    /// <summary>
    /// The part's content type. The server files an upload as an image only when the type says
    /// so; its form parser would otherwise assume text.
    /// </summary>
    public static string ContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".bmp" => "image/bmp",
        ".ico" => "image/x-icon",
        ".tif" or ".tiff" => "image/tiff",
        ".avif" => "image/avif",
        _ => "application/octet-stream",
    };

    [GeneratedRegex(@"[\s,;#]+")]
    private static partial Regex Separators();

    [GeneratedRegex(@"\.{2,}")]
    private static partial Regex Dots();
}
