using System.Text;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>What a clipboard tool needs from Windows; the runner passes the real one, tests a fake.</summary>
public interface IToolHost
{
    /// <summary><c>\\server\share\…</c> for a path on a mapped network drive; null for any other path.</summary>
    string? UniversalName(string path);

    /// <summary>
    /// The file's bytes, but no more than <paramref name="maxBytes"/> + 1: one byte over says the
    /// file is too large, without reading a file that grew since it was selected, or the target
    /// of a link, to its end.
    /// </summary>
    byte[] ReadFile(string path, long maxBytes);

    /// <summary>The Windows code page, for a text file that has no byte order mark and is not UTF-8.</summary>
    Encoding Ansi { get; }

    /// <summary>The file's "Title" property as Explorer shows it (a song's, a document's); null when it has none.</summary>
    string? Title(string path);
}

/// <param name="Text">What goes on the clipboard; null when nothing should.</param>
/// <param name="Errors">Why nothing was copied, one line per item.</param>
public sealed record ToolResult(string? Text, IReadOnlyList<string> Errors);

/// <summary>
/// The text the clipboard tools copy (ADR-0015). Lines are separated by CRLF, the way Windows
/// programs paste them, and the text does not end with one: a path pasted into a console must
/// not run it.
/// </summary>
public static class ClipboardTools
{
    /// <summary>Copy content refuses more than this, summed over the selection.</summary>
    public const int MaxContentMegabytes = 16;

    public const long MaxContentBytes = MaxContentMegabytes * 1024L * 1024;

    private const string NewLine = "\r\n";

    public static ToolResult Run(BuiltinTool tool, IReadOnlyList<SelectedItem> items, IToolHost host) => tool switch
    {
        BuiltinTool.CopyPath => Lines(items.Select(item => item.Path)),
        BuiltinTool.CopyName => Lines(items.Select(NameOf)),
        BuiltinTool.CopyUncPath => Lines(items.Select(item => host.UniversalName(item.Path) ?? item.Path)),
        BuiltinTool.CopyContent => Content(items, host),
        BuiltinTool.CopyInternetPath => Lines(items.Select(item => InternetPath(item.Path))),
        BuiltinTool.CopyTitle => Titles(items, host),
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null),
    };

    /// <summary>
    /// <c>C:\a b\ş.txt</c> → <c>file:///C:/a%20b/%C5%9F.txt</c>; <c>\\server\share\x</c> →
    /// <c>file://server/share/x</c>. Built by hand: on Linux, where the tests run, Uri reads
    /// <c>C:</c> as a scheme.
    /// </summary>
    public static string InternetPath(string path)
    {
        bool unc = path.StartsWith(@"\\", StringComparison.Ordinal);
        string[] segments = path.TrimStart('\\').Split('\\');
        IEnumerable<string> escaped = segments.Select((segment, index) => !unc && index == 0 ? segment : Uri.EscapeDataString(segment));
        return (unc ? "file://" : "file:///") + string.Join('/', escaped);
    }

    /// <summary>The last path segment; a drive or share root, which has none, as <c>C:</c> or <c>\\server\share</c>.</summary>
    private static string NameOf(SelectedItem item) =>
        item.Kind == ItemKind.Drive || WindowsPath.IsRoot(item.Path)
            ? item.Path.TrimEnd('\\')
            : item.Name;

    private static ToolResult Lines(IEnumerable<string> lines) => new(string.Join(NewLine, lines), []);

    /// <summary>All or nothing, as copy content: a missing title would shift every line after it.</summary>
    private static ToolResult Titles(IReadOnlyList<SelectedItem> items, IToolHost host)
    {
        var titles = new List<string>();
        var errors = new List<string>();
        foreach (SelectedItem item in items)
        {
            if (item.Kind == ItemKind.File && host.Title(item.Path) is { Length: > 0 } title)
            {
                titles.Add(title);
            }
            else
            {
                errors.Add(Strings.ToolNoTitle(item.Path));
            }
        }

        return errors.Count > 0 ? new ToolResult(null, errors) : Lines(titles);
    }

    private static ToolResult Content(IReadOnlyList<SelectedItem> items, IToolHost host)
    {
        // All or nothing: a clipboard holding part of what was asked for is easy to paste unnoticed.
        var errors = new List<string>();
        var texts = new List<string>();
        long total = 0;
        foreach (SelectedItem item in items)
        {
            if (item.Kind != ItemKind.File)
            {
                errors.Add(Strings.ToolNotAFile(item.Path));
                continue;
            }

            try
            {
                long remaining = MaxContentBytes - total;
                byte[] bytes = host.ReadFile(item.Path, remaining);
                if (bytes.Length > remaining)
                {
                    return new ToolResult(null, [Strings.ToolContentTooLarge(MaxContentMegabytes)]);
                }

                total += bytes.Length;
                if (TextDecoder.Decode(bytes, host.Ansi) is { } text)
                {
                    texts.Add(text);
                }
                else
                {
                    errors.Add(Strings.ToolNotText(item.Path));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(item.Path, ex.Message));
            }
        }

        return errors.Count > 0
            ? new ToolResult(null, errors)
            : new ToolResult(string.Join(NewLine, texts), []);
    }
}

/// <summary>Reads a text file the way Notepad guesses it: byte order mark, then UTF-8, then the Windows code page.</summary>
public static class TextDecoder
{
    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <returns>
    /// The text, or null when it holds a NUL character: a binary file, or a text file with a
    /// zero-filled block. The clipboard's text ends at the first NUL, so whatever follows it would
    /// be lost without a word.
    /// </returns>
    public static string? Decode(ReadOnlySpan<byte> bytes, Encoding ansi)
    {
        string text = DecodeAny(bytes, ansi);
        return text.Contains('\0') ? null : text;
    }

    private static string DecodeAny(ReadOnlySpan<byte> bytes, Encoding ansi)
    {
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
        {
            return Encoding.UTF8.GetString(bytes[3..]);
        }

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE]))
        {
            return Encoding.Unicode.GetString(bytes[2..]);
        }

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF]))
        {
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        }

        // A NUL byte is valid UTF-8; the caller turns it away after decoding.
        try
        {
            return s_strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return ansi.GetString(bytes);
        }
    }
}
