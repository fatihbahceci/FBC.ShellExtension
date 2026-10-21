using System.ComponentModel;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Runner.UI;

/// <summary>
/// Asks for the part size before a split starts, in a <see cref="TextPrompt"/> with the usual
/// sizes as presets.
/// </summary>
internal static class SplitPrompt
{
    // Keeps "Split <name> (<size>) into parts of:" within the two lines the question has.

    private const int DefaultPreset = 2;
    private static readonly string[] Presets = ["10 MB", "25 MB", "100 MB", "700 MB", "1 GB", "4095 MB"];

    /// <returns>The part size in bytes, or null when the user cancelled.</returns>
    /// <exception cref="Win32Exception">The window could not be created.</exception>
    public static long? Ask(IReadOnlyList<SelectedItem> items, string title)
    {
        // The prompt closes only on a size that parses, so the last one parsed is the answer.
        long bytes = 0;
        var options = new TextPromptOptions(title, Question(items), Presets[DefaultPreset], Strings.SplitPromptHint(),
            Presets, Strings.SplitPromptOk(), text => SizeText.TryParse(text, out bytes) ? null : Strings.SplitPromptInvalid(text));
        return TextPrompt.Ask(options) is null ? null : bytes;
    }

    /// <summary>The name and size of a single file; only the count for several, or when the size cannot be read.</summary>
    private static string Question(IReadOnlyList<SelectedItem> items)
    {
        if (items.Count == 1)
        {
            try
            {
                long length = new FileInfo(items[0].Path).Length;
                return Strings.SplitPromptOne(TextPrompt.ShortName(items[0].Name), SizeText.Format(length));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // The split itself reports why the file cannot be read; the question does without the size.
            }
        }

        return Strings.SplitPromptMany(items.Count);
    }
}
