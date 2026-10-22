using System.Globalization;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>How Advanced rename changes the case of the new names.</summary>
public enum NameCase
{
    Unchanged,
    Lower,
    Upper,

    /// <summary>Each word starting with a capital, the rest small.</summary>
    Title,
}

/// <summary>
/// What Advanced rename does to every name, in this order: find and replace in the name without
/// its extension, the templates for the name and the extension, then the case.
/// </summary>
/// <param name="NameTemplate">The new name without its extension: <c>{name}</c>, <c>{n}</c>, <c>{parent}</c>, <c>{date}</c> or <c>{date:yyyyMMdd}</c>, and text.</param>
/// <param name="ExtensionTemplate">The new extension, without the dot; <c>{ext}</c> keeps it. Folders have none.</param>
/// <param name="Start">The first <c>{n}</c>.</param>
/// <param name="Step">What <c>{n}</c> grows by from one item to the next.</param>
/// <param name="Digits">The least digits of <c>{n}</c>, with leading zeros.</param>
public sealed record RenameRules(string Find, string Replace, bool MatchCase, bool RegularExpression,
    string NameTemplate, string ExtensionTemplate, NameCase Case, int Start, int Step, int Digits)
{
    /// <summary>Every name as it is.</summary>
    public static RenameRules Unchanged { get; } = new("", "", false, false, "{name}", "{ext}", NameCase.Unchanged, 1, 1, 1);
}

/// <summary>One item of Advanced rename's preview: its new name, or why it cannot have it.</summary>
public sealed record RenameRow(SelectedItem Item, string NewName, string? Problem)
{
    /// <summary>The name changes, case alone too, and nothing stands in the way.</summary>
    public bool Changes => Problem is null && !string.Equals(NewName, Item.Name, StringComparison.Ordinal);
}

/// <summary>The new names; <see cref="Problem"/> when the rules themselves are wrong (a bad regular expression).</summary>
public sealed record RenamePreview(IReadOnlyList<RenameRow> Rows, string? Problem)
{
    /// <summary>All or nothing: a single problem keeps every name, so the numbering is not left half done.</summary>
    public bool CanRename => Problem is null && Rows.All(row => row.Problem is null) && Rows.Any(row => row.Changes);
}

/// <summary>Advanced rename's plan (ADR-0029): the names, their problems and the order to rename in.</summary>
public static partial class FileTools
{
    private static readonly TimeSpan s_regexTimeout = TimeSpan.FromSeconds(1);

    // A name Windows takes, and a path the shell's engine reaches without long-path support.
    private const int MaxName = 255;
    private const int MaxPath = 259;

    /// <summary>The order the items are numbered in: folder by folder, by name as Explorer sorts (2 before 10).</summary>
    public static IReadOnlyList<SelectedItem> RenameOrder(IEnumerable<SelectedItem> items) =>
        [.. items.OrderBy(item => item.Directory, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Name, NaturalComparer.Instance)];

    /// <summary>The new name of every item, in the given order, and what stands in the way of each.</summary>
    /// <param name="exists">Whether a path is taken, by a file or a folder.</param>
    /// <param name="modified">An item's modified date, for <c>{date}</c>.</param>
    public static RenamePreview PreviewRename(IReadOnlyList<SelectedItem> items, RenameRules rules, Func<string, bool> exists, Func<string, DateTime> modified)
    {
        IReadOnlyList<RenameRow> unchanged = [.. items.Select(item => new RenameRow(item, item.Name, null))];
        Regex? find = null;
        if (rules.Find.Length > 0)
        {
            try
            {
                find = new Regex(rules.RegularExpression ? rules.Find : Regex.Escape(rules.Find),
                    RegexOptions.CultureInvariant | (rules.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase), s_regexTimeout);
            }
            catch (ArgumentException ex)
            {
                return new RenamePreview(unchanged, Strings.ToolRegexInvalid(ex.Message));
            }
        }

        if ((TemplateProblem(rules.NameTemplate) ?? TemplateProblem(rules.ExtensionTemplate)) is { } templateProblem)
        {
            return new RenamePreview(unchanged, templateProblem);
        }

        // Plain text is replaced as it is: a $ in it is not a group.
        string replacement = rules.RegularExpression ? rules.Replace : rules.Replace.Replace("$", "$$", StringComparison.Ordinal);
        var rows = new List<RenameRow>();
        for (int index = 0; index < items.Count; index++)
        {
            SelectedItem item = items[index];
            string newName;
            try
            {
                newName = NewName(item, rules, find, replacement, rules.Start + (index * rules.Step), modified);
            }
            catch (RegexMatchTimeoutException)
            {
                // The next name would take as long: the preview runs on every keystroke.
                return new RenamePreview(unchanged, Strings.RenameRegexTooSlow());
            }

            string? problem = !WindowsPath.IsValidName(newName) ? Strings.RenameProblemInvalid()
                : newName.Length > MaxName || PathIn(item.Directory, newName).Length > MaxPath ? Strings.RenameProblemTooLong()
                : null;
            rows.Add(new RenameRow(item, newName, problem));
        }

        return new RenamePreview(MarkCycles(MarkTaken(rows, exists)), null);
    }

    /// <summary>
    /// The changing rows of a preview that <see cref="RenamePreview.CanRename"/>, in an order where
    /// each new name is free when its turn comes: a → b only after b → c.
    /// </summary>
    public static IReadOnlyList<RenameRow> RenameSequence(RenamePreview preview)
    {
        List<RenameRow> pending = [.. preview.Rows.Where(row => row.Changes)];
        var taken = new HashSet<string>(pending.Select(row => row.Item.Path), StringComparer.OrdinalIgnoreCase);
        var sequence = new List<RenameRow>();
        while (pending.Count > 0)
        {
            // MarkCycles left no loop, so one row is always free to go. The deepest goes first: an
            // item inside a renamed folder is reached by its old path only before the folder goes.
            RenameRow next = pending.Where(row => !taken.Contains(Target(row)) || IsSelf(row))
                .OrderByDescending(row => row.Item.Path.Count(c => c is '\\' or '/'))
                .First();
            sequence.Add(next);
            pending.Remove(next);
            taken.Remove(next.Item.Path);
        }

        return sequence;
    }

    private static string NewName(SelectedItem item, RenameRules rules, Regex? find, string replacement, int number, Func<string, DateTime> modified)
    {
        string name = item.Name;
        string extension = item.Kind == ItemKind.File ? WindowsPath.GetExtension(name) : "";
        if (extension.Length == name.Length)
        {
            // ".gitignore" is all name.
            extension = "";
        }

        string stem = name[..^extension.Length];
        string replaced = find?.Replace(stem, replacement) ?? stem;
        var values = new TemplateValues(replaced, extension.TrimStart('.'), number, rules.Digits, WindowsPath.GetFileName(item.Directory), () => modified(item.Path));
        string newStem = ChangeCase(Expand(rules.NameTemplate, values), rules.Case);

        // Title case is for words; an extension then goes small, as most are written.
        string newExtension = item.Kind == ItemKind.File
            ? ChangeCase(Expand(rules.ExtensionTemplate, values), rules.Case == NameCase.Title ? NameCase.Lower : rules.Case)
            : "";
        return newExtension.Length > 0 ? $"{newStem}.{newExtension}" : newStem;
    }

    private sealed record TemplateValues(string Name, string Extension, int Number, int Digits, string Parent, Func<DateTime> Modified);

    private static readonly Regex s_token = new(@"\{(?<name>[a-z]+)(?::(?<format>[^{}]*))?\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Why the template cannot be used: a token it does not know, or a date format .NET cannot write.</summary>
    private static string? TemplateProblem(string template)
    {
        foreach (Match token in s_token.Matches(template))
        {
            string name = token.Groups["name"].Value.ToLowerInvariant();
            if (name is not ("name" or "ext" or "n" or "parent" or "date") || (token.Groups["format"].Success && name != "date"))
            {
                return Strings.RenameTokenUnknown(token.Value);
            }

            if (token.Groups["format"].Success)
            {
                try
                {
                    _ = DateTime.Now.ToString(token.Groups["format"].Value, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    return Strings.RenameDateFormatInvalid(token.Groups["format"].Value);
                }
            }
        }

        return null;
    }

    private static string Expand(string template, TemplateValues values) =>
        s_token.Replace(template, token => token.Groups["name"].Value.ToLowerInvariant() switch
        {
            "name" => values.Name,
            "ext" => values.Extension,
            "n" => values.Number.ToString($"D{Math.Clamp(values.Digits, 1, 9)}", CultureInfo.InvariantCulture),
            "parent" => values.Parent,
            _ => values.Modified().ToString(token.Groups["format"].Success ? token.Groups["format"].Value : "yyyy-MM-dd", CultureInfo.InvariantCulture),
        });

    /// <summary>
    /// Without the user's language: in Turkish "FILE.GIF" would go to "fıle.gıf", and an extension
    /// that is no longer the file's type. Windows compares names this way too.
    /// </summary>
    private static string ChangeCase(string text, NameCase nameCase)
    {
        TextInfo culture = CultureInfo.InvariantCulture.TextInfo;
        return nameCase switch
        {
            NameCase.Lower => culture.ToLower(text),
            NameCase.Upper => culture.ToUpper(text),
            NameCase.Title => culture.ToTitleCase(culture.ToLower(text)),
            _ => text,
        };
    }

    private static string Target(RenameRow row) => PathIn(row.Item.Directory, row.NewName);

    /// <summary>A case change only: the new path is the item's own.</summary>
    private static bool IsSelf(RenameRow row) => string.Equals(Target(row), row.Item.Path, StringComparison.OrdinalIgnoreCase);

    private static string PathIn(string folder, string name) =>
        folder.EndsWith('\\') || folder.EndsWith('/') ? folder + name : $"{folder}\\{name}";

    /// <summary>Two items given one name, or a name an item outside the selection has.</summary>
    private static List<RenameRow> MarkTaken(List<RenameRow> rows, Func<string, bool> exists)
    {
        var renamed = new HashSet<string>(rows.Where(row => row.Changes).Select(row => row.Item.Path), StringComparer.OrdinalIgnoreCase);
        var shared = new HashSet<string>(
            rows.Where(row => row.Problem is null).GroupBy(Target, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key),
            StringComparer.OrdinalIgnoreCase);
        return [.. rows.Select(row =>
            row.Problem is not null ? row
            : shared.Contains(Target(row)) ? row with { Problem = Strings.RenameProblemDuplicate() }
            : row.Changes && !IsSelf(row) && !renamed.Contains(Target(row)) && exists(Target(row)) ? row with { Problem = Strings.RenameProblemExists() }
            : row)];
    }

    /// <summary>
    /// Names that go round (a → b, b → a): neither can go first. Marked, not solved with a
    /// temporary name: a half-done swap would leave a name nobody chose.
    /// </summary>
    private static List<RenameRow> MarkCycles(List<RenameRow> rows)
    {
        // Two paths that differ in case alone are one item to Windows, except in a case-sensitive folder.
        var byPath = new Dictionary<string, RenameRow>(StringComparer.OrdinalIgnoreCase);
        foreach (RenameRow row in rows.Where(row => row.Changes))
        {
            byPath.TryAdd(row.Item.Path, row);
        }

        return [.. rows.Select(row => row.Changes && InCycle(row, byPath) ? row with { Problem = Strings.RenameProblemSwap() } : row)];
    }

    private static bool InCycle(RenameRow start, Dictionary<string, RenameRow> byPath)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RenameRow row = start;
        while (!IsSelf(row) && byPath.TryGetValue(Target(row), out RenameRow? next) && seen.Add(row.Item.Path))
        {
            if (ReferenceEquals(next, start))
            {
                return true;
            }

            row = next;
        }

        return false;
    }

    /// <summary>Names as Explorer sorts them: a run of digits by its value, so "2" comes before "10".</summary>
    public sealed class NaturalComparer : IComparer<string>
    {
        public static NaturalComparer Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            if (x is null || y is null)
            {
                return x is null ? (y is null ? 0 : -1) : 1;
            }

            int i = 0;
            int j = 0;
            while (i < x.Length && j < y.Length)
            {
                int result;
                if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
                {
                    int xEnd = RunEnd(x, i, digits: true);
                    int yEnd = RunEnd(y, j, digits: true);
                    ReadOnlySpan<char> xNumber = x.AsSpan(i, xEnd - i).TrimStart('0');
                    ReadOnlySpan<char> yNumber = y.AsSpan(j, yEnd - j).TrimStart('0');
                    result = xNumber.Length != yNumber.Length ? xNumber.Length.CompareTo(yNumber.Length) : xNumber.CompareTo(yNumber, StringComparison.Ordinal);
                    (i, j) = (xEnd, yEnd);
                }
                else
                {
                    int xEnd = RunEnd(x, i, digits: false);
                    int yEnd = RunEnd(y, j, digits: false);
                    result = string.Compare(x[i..xEnd], y[j..yEnd], CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
                    (i, j) = (xEnd, yEnd);
                }

                if (result != 0)
                {
                    return result;
                }
            }

            // What is left decides; equal values written differently (01 and 1) still have an order.
            int remaining = (x.Length - i).CompareTo(y.Length - j);
            return remaining != 0 ? remaining : string.CompareOrdinal(x, y);
        }

        private static int RunEnd(string text, int start, bool digits)
        {
            int end = start;
            while (end < text.Length && char.IsAsciiDigit(text[end]) == digits)
            {
                end++;
            }

            return end;
        }
    }
}
