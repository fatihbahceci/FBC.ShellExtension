using System.Text;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.WikiImport;

/// <summary>
/// A Wiki.js 2 page named by its address, as the browser shows it:
/// <c>https://wiki.example.com:19888/en/internal/third-party/page</c>.
/// </summary>
/// <param name="BaseUrl">Scheme, host and port, without a trailing slash.</param>
/// <param name="Locale">The locale the address names; null when it names none, and the site's default applies.</param>
/// <param name="Path">The page path, without a slash at either end: <c>internal/third-party/page</c>.</param>
public sealed partial record WikiPageAddress(string BaseUrl, string? Locale, string Path)
{
    // Top-level routes the server answers itself (server/controllers/common.js); a page there is unreachable.
    private static readonly string[] s_reserved = ["login", "logout", "register", "verify", "favicons", "fonts", "img", "js", "svg"];

    // Characters the official media dialog refuses in a folder name, and the quote the server's
    // sanitize-filename drops; uppercase is lowered instead.
    private static readonly char[] s_badSlugCharacters = "()=.!@#$%?&*+`~<>,;:\\/[]¬{| \"".ToCharArray();

    /// <summary>
    /// The asset folders the images go into: the page path's segments as the server stores them
    /// (ADR-0020, the path rule; ADR-0021).
    /// </summary>
    public IReadOnlyList<string> FolderSlugs => [.. Path.Split('/').Select(ServerSlug)];

    /// <summary>An asset's link: <c>/internal/third-party/page/image.png</c>, or with the base URL in front.</summary>
    public string AssetLink(string fileName, bool fullUrl) =>
        (fullUrl ? BaseUrl : "") + "/" + string.Join('/', FolderSlugs) + "/" + fileName;

    /// <summary>The page's address with the locale it was saved in.</summary>
    public string PageUrl(string locale) => $"{BaseUrl}/{locale}/{Path}";

    /// <summary>
    /// The wiki an address belongs to, as <see cref="BaseUrl"/> writes it: scheme, host and port.
    /// Any page of the wiki will do, or the wiki's own address; null for what is not a web address.
    /// </summary>
    public static string? BaseUrlOf(string text) =>
        Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && uri.Host.Length > 0
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;

    /// <summary>Why <paramref name="text"/> is not a page address this tool can use; null when it is one.</summary>
    public static string? Problem(string text) => TryParse(text, out _, out string? problem) ? null : problem;

    /// <exception cref="FormatException">Not a page address this tool can use; the message says why.</exception>
    public static WikiPageAddress Parse(string text) =>
        TryParse(text, out WikiPageAddress? address, out string? problem) ? address : throw new FormatException(problem);

    /// <summary>
    /// A first segment that looks like a locale (<c>en</c>, <c>pt-br</c>) is the locale: the
    /// server strips such a segment from every path, so no page or asset folder can start with
    /// one. The rest is checked against what the server and its media dialog accept.
    /// </summary>
    public static bool TryParse(string text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out WikiPageAddress? address, out string? problem)
    {
        address = null;
        if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.Host.Length == 0)
        {
            problem = Strings.WikiAddressNotUrl(text.Trim());
            return false;
        }

        string[] segments = [.. uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString)];
        string? locale = null;
        if (segments.Length > 0 && LocalePattern().IsMatch(segments[0]))
        {
            locale = segments[0].ToLowerInvariant();
            segments = segments[1..];
        }

        if (segments.Length == 0)
        {
            problem = Strings.WikiAddressNoPath();
            return false;
        }

        if (segments[0].Length == 1 || s_reserved.Contains(segments[0], StringComparer.OrdinalIgnoreCase) || LocalePattern().IsMatch(segments[0]))
        {
            problem = Strings.WikiAddressFirstSegment(segments[0]);
            return false;
        }

        if (segments.FirstOrDefault(segment => segment.IndexOfAny(s_badSlugCharacters) >= 0 || segment.Any(char.IsControl)) is { } bad)
        {
            problem = Strings.WikiAddressBadSegment(bad);
            return false;
        }

        // sanitize-filename turns a Windows device name into an empty folder name.
        if (segments.FirstOrDefault(segment => DeviceName().IsMatch(segment)) is { } device)
        {
            problem = Strings.WikiAddressDeviceName(device);
            return false;
        }

        address = new WikiPageAddress(uri.GetLeftPart(UriPartial.Authority), locale, string.Join('/', segments));
        problem = null;
        return true;
    }

    /// <summary>
    /// The slug the server stores for a segment: <c>slug.toLowerCase()</c> in JavaScript, which
    /// follows Unicode's full mapping where .NET's invariant lowering does not. İ becomes i and a
    /// combining dot (U+0307), a word-final Σ becomes ς; anything else lowers the same in both.
    /// </summary>
    private static string ServerSlug(string segment)
    {
        var slug = new StringBuilder(segment.Length + 1);
        for (int index = 0; index < segment.Length; index++)
        {
            char c = segment[index];
            slug.Append(c switch
            {
                '\u0130' => "i\u0307",
                '\u03A3' when index > 0 && char.IsLetter(segment[index - 1]) && (index + 1 == segment.Length || !char.IsLetter(segment[index + 1])) => "\u03C2",
                _ => c.ToString(),
            });
        }

        return slug.ToString().ToLowerInvariant();
    }

    [GeneratedRegex("^[a-z]{2}(-[a-z]{2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex LocalePattern();

    [GeneratedRegex("^(con|prn|aux|nul|com[0-9]|lpt[0-9])$", RegexOptions.IgnoreCase)]
    private static partial Regex DeviceName();
}
