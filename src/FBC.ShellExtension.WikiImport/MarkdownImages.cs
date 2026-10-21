using System.Text;
using System.Text.RegularExpressions;

namespace FBC.ShellExtension.WikiImport;

/// <summary>An image a markdown file links to, and where its link sits in the text.</summary>
/// <param name="Start">Where the link's target starts in the markdown.</param>
/// <param name="Length">The target's length; a new target replaces exactly this text.</param>
/// <param name="Target">The target as written: <c>a.assets/x%20y.png</c>, without angle brackets.</param>
/// <param name="IsHtml">From an <c>&lt;img src&gt;</c>; otherwise from <c>![alt](target "title")</c>.</param>
/// <param name="InAngleBrackets">Written as <c>![alt](&lt;target&gt;)</c>; the brackets stay.</param>
public sealed record ImageReference(int Start, int Length, string Target, bool IsHtml, bool InAngleBrackets = false);

/// <summary>
/// Finds the images a markdown file (Typora's among them) links to and points the links at new
/// targets, changing nothing else: the alt text, a <c>"title"</c>, and an <c>&lt;img&gt;</c>'s
/// other attributes (Typora writes <c>style="zoom:50%"</c> when an image is resized) stay as
/// they were. Fenced code blocks and code spans are left alone; a block indented as code
/// without a fence is not told apart from a list item's text.
/// </summary>
public static partial class MarkdownImages
{
    public static IReadOnlyList<ImageReference> Find(string markdown)
    {
        List<(int Start, int End)> code = CodeRanges(markdown);
        var found = new List<ImageReference>();
        foreach (Match match in Outside(MarkdownImage(), markdown, code))
        {
            Group angle = match.Groups["angle"];
            Group target = angle.Success ? angle : match.Groups["plain"];
            found.Add(new ImageReference(target.Index, target.Length, target.Value, IsHtml: false, InAngleBrackets: angle.Success));
        }

        foreach (Match match in Outside(HtmlImage(), markdown, code))
        {
            Group source = new[] { match.Groups["dq"], match.Groups["sq"], match.Groups["uq"] }.First(group => group.Success);
            found.Add(new ImageReference(source.Index, source.Length, source.Value, IsHtml: true));
        }

        // ![alt][label], ![label][] and ![label] take their target from a "[label]: target" line;
        // the line is rewritten, once for all the images that use it.
        var labels = new HashSet<string>(
            Outside(ReferenceImage(), markdown, code).Select(match => Label(match.Groups["label"] is { Success: true, Length: > 0 } label ? label.Value : match.Groups["alt"].Value)),
            StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Outside(Definition(), markdown, code))
        {
            if (labels.Contains(Label(match.Groups["label"].Value)))
            {
                Group angle = match.Groups["angle"];
                Group target = angle.Success ? angle : match.Groups["plain"];
                found.Add(new ImageReference(target.Index, target.Length, target.Value, IsHtml: false, InAngleBrackets: angle.Success));
            }
        }

        return [.. found.DistinctBy(reference => reference.Start).OrderBy(reference => reference.Start)];
    }

    /// <summary>
    /// The matches that do not start in code. A match that does is dropped and the search goes
    /// on after the code, not after the match: an unclosed <c>![</c> in a code span would
    /// otherwise swallow the image that follows it.
    /// </summary>
    private static IEnumerable<Match> Outside(Regex regex, string markdown, List<(int Start, int End)> code)
    {
        Match match = regex.Match(markdown);
        while (match.Success)
        {
            int codeEnd = -1;
            foreach ((int start, int end) in code)
            {
                if (match.Index >= start && match.Index < end)
                {
                    codeEnd = end;
                    break;
                }
            }

            if (codeEnd >= 0)
            {
                match = regex.Match(markdown, codeEnd);
                continue;
            }

            yield return match;
            match = match.NextMatch();
        }
    }

    /// <summary>Labels match without case and with runs of white space as one space.</summary>
    private static string Label(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The file a target names, relative to the markdown file's folder; null for an address on the
    /// web (http, https), inline data, an anchor, or a path that starts at a root Typora alone
    /// knows. Typora encodes a space as <c>%20</c>; such escapes are decoded.
    /// </summary>
    public static string? LocalPath(string target, string markdownFolder)
    {
        if (target.Length == 0 || target[0] == '#')
        {
            return null;
        }

        if (target.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) && uri.IsFile ? uri.LocalPath : null;
        }

        if (SchemePattern().IsMatch(target))
        {
            return null;
        }

        string decoded = Uri.UnescapeDataString(target);
        if (WindowsAbsolute().IsMatch(decoded))
        {
            return decoded;
        }

        if (decoded.StartsWith('/') || decoded.StartsWith('\\'))
        {
            return null;
        }

        string relative = decoded.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(markdownFolder, relative));
    }

    /// <summary>The markdown with every reference in <paramref name="targets"/> pointing at its new target.</summary>
    public static string Rewrite(string markdown, IReadOnlyDictionary<ImageReference, string> targets)
    {
        var text = new StringBuilder(markdown);
        foreach ((ImageReference reference, string target) in targets.OrderByDescending(pair => pair.Key.Start))
        {
            // A markdown target with a space or a parenthesis needs angle brackets around it.
            string written = !reference.IsHtml && !reference.InAngleBrackets && target.IndexOfAny([' ', '(', ')']) >= 0
                ? $"<{target}>"
                : target;
            text.Remove(reference.Start, reference.Length).Insert(reference.Start, written);
        }

        return text.ToString();
    }

    /// <summary>
    /// The page title the file gives, and its text without what Wiki.js shows on its own: a
    /// YAML front matter block (Typora writes one on request; its <c>title</c> is used) and,
    /// when <paramref name="removeFirstHeading"/>, a level-1 heading the text opens with (<c>#
    /// Title</c>, or <c>Title</c> over <c>===</c>), since Wiki.js shows the title above the page
    /// anyway. A level-1 heading further down is a part of the text, not its title.
    /// </summary>
    /// <returns>The title, or null when the file names none; the content.</returns>
    public static (string? Title, string Content) TakeTitle(string markdown, bool removeFirstHeading)
    {
        string? title = null;
        string content = markdown;
        if (FrontMatter().Match(content) is { Success: true } front)
        {
            Match line = FrontMatterTitle().Match(front.Groups["yaml"].Value);
            title = line.Success ? line.Groups["title"].Value.Trim().Trim('"', '\'') : null;
            content = content[front.Length..];
        }

        if (LeadingHeading().Match(content) is { Success: true } heading)
        {
            title ??= (heading.Groups["atx"].Success ? heading.Groups["atx"] : heading.Groups["setext"]).Value.Trim();
            if (removeFirstHeading)
            {
                content = content[heading.Length..];
            }
        }

        return (string.IsNullOrWhiteSpace(title) ? null : title, content.TrimStart('\r', '\n'));
    }

    /// <summary>
    /// Fenced code blocks (``` and ~~~, at any indent, in a block quote too) and code spans, as
    /// start and end indexes.
    /// </summary>
    private static List<(int Start, int End)> CodeRanges(string markdown)
    {
        var ranges = new List<(int Start, int End)>();
        int index = 0;
        (int Start, string Fence)? open = null;
        foreach (string line in markdown.Split('\n'))
        {
            Match fence = Fence().Match(line);

            // A backtick fence's info text has no backtick: ```a``` is a code span.
            if (fence.Success && fence.Groups["fence"].Value[0] == '`' && fence.Groups["info"].Value.Contains('`'))
            {
                fence = Match.Empty;
            }

            if (open is { } block)
            {
                if (fence.Success && fence.Groups["fence"].Value.StartsWith(block.Fence, StringComparison.Ordinal) && fence.Groups["info"].Value.Trim().Length == 0)
                {
                    ranges.Add((block.Start, index + line.Length));
                    open = null;
                }
            }
            else if (fence.Success)
            {
                open = (index, fence.Groups["fence"].Value);
            }
            else
            {
                foreach (Match span in CodeSpan().Matches(line))
                {
                    ranges.Add((index + span.Index, index + span.Index + span.Length));
                }
            }

            index += line.Length + 1;
        }

        if (open is { } unclosed)
        {
            ranges.Add((unclosed.Start, markdown.Length));
        }

        return ranges;
    }

    // ![alt](target "title"): the alt text may hold one level of [brackets]; the target is <…>,
    // or text without spaces in which parentheses pair up; the title may hold escaped quotes.
    [GeneratedRegex("""!\[(?:[^\[\]\\\r\n]|\\.|\[(?:[^\[\]\\\r\n]|\\.)*\])*\]\(\s*(?:<(?<angle>[^>\r\n]*)>|(?<plain>(?:[^\s()\\]|\\.|\((?:[^\s()\\]|\\.)*\))+))(?:\s+(?:"(?:[^"\\\r\n]|\\.)*"|'(?:[^'\\\r\n]|\\.)*'|\((?:[^()\\\r\n]|\\.)*\)))?\s*\)""")]
    private static partial Regex MarkdownImage();

    // ![alt][label], ![label][], ![label]: an image whose target is defined elsewhere.
    [GeneratedRegex("""!\[(?<alt>(?:[^\[\]\\\r\n]|\\.|\[(?:[^\[\]\\\r\n]|\\.)*\])*)\](?:\[(?<label>[^\[\]\r\n]*)\]|(?![(\[]))""")]
    private static partial Regex ReferenceImage();

    // [label]: target "title"
    [GeneratedRegex("""^ {0,3}\[(?<label>[^\[\]\r\n]+)\]:[ \t]*(?:<(?<angle>[^>\r\n]*)>|(?<plain>[^\s<][^\s]*))""", RegexOptions.Multiline)]
    private static partial Regex Definition();

    [GeneratedRegex("""<img\b[^>]*?\ssrc\s*=\s*(?:"(?<dq>[^"]*)"|'(?<sq>[^']*)'|(?<uq>[^\s"'>]+))""", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlImage();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]+:")]
    private static partial Regex SchemePattern();

    [GeneratedRegex(@"^([A-Za-z]:[\\/]|\\\\)")]
    private static partial Regex WindowsAbsolute();

    [GeneratedRegex(@"^(?:[ \t]*>)*[ \t]*(?<fence>`{3,}|~{3,})(?<info>[^\r]*)")]
    private static partial Regex Fence();

    [GeneratedRegex(@"(?<ticks>`+)[^`\r\n](?:.*?[^`])??\k<ticks>(?!`)")]
    private static partial Regex CodeSpan();

    [GeneratedRegex(@"^---[ \t]*\r?\n(?<yaml>[\s\S]*?)\r?\n---[ \t]*(\r?\n|$)")]
    private static partial Regex FrontMatter();

    [GeneratedRegex(@"^title:(?<title>.*)$", RegexOptions.Multiline)]
    private static partial Regex FrontMatterTitle();

    // After blank lines only: "# Title", or a line that starts no other block over "===".
    [GeneratedRegex(@"\A(?:[ \t]*\r?\n)*(?: {0,3}#[ \t]+(?<atx>[^\r\n]+?)(?:[ \t]+#+)?[ \t]*|(?![ \t]*(?:[#>|<]|[-*+][ \t]|\d+[.)][ \t]|```|~~~))(?<setext>[^\r\n]*\S[^\r\n]*)\r?\n {0,3}=+[ \t]*)(?:\r?\n|\z)")]
    private static partial Regex LeadingHeading();
}
