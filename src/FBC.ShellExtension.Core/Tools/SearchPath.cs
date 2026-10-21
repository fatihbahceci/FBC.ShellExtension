namespace FBC.ShellExtension.Core.Tools;

/// <summary>
/// PATH lists, with Windows rules: ';'-separated, case-insensitive, a trailing backslash does not
/// make a different folder.
/// </summary>
public static class SearchPath
{
    public static IReadOnlyList<string> Split(string? path) =>
        [.. (path ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static string Key(string folder) => folder.TrimEnd('\\', '/').ToUpperInvariant();

    /// <summary>
    /// The PATH the runner gives its commands: the machine's and the user's current entries from
    /// the registry first, so an install that happened after its host started is seen; then
    /// whatever else the host had; and, in front of all, the program's own tools folder.
    /// </summary>
    public static string Merge(string? machine, string? user, string? inherited, string? toolsFolder)
    {
        var seen = new HashSet<string>();
        var result = new List<string>();
        void Add(string folder)
        {
            if (seen.Add(Key(folder)))
            {
                result.Add(folder);
            }
        }

        if (!string.IsNullOrWhiteSpace(toolsFolder))
        {
            Add(toolsFolder);
        }

        foreach (string folder in Split(machine).Concat(Split(user)).Concat(Split(inherited)))
        {
            Add(folder);
        }

        return string.Join(';', result);
    }

    public static bool Contains(string? path, string folder) => Split(path).Any(entry => Key(entry) == Key(folder));

    /// <summary>The user's PATH with the folder appended, unchanged when it is there already.</summary>
    public static string WithEntry(string? path, string folder) =>
        Contains(path, folder) ? path ?? "" : string.Join(';', Split(path).Append(folder));

    /// <summary>The user's PATH without the folder; other entries keep their order and spelling.</summary>
    public static string WithoutEntry(string? path, string folder) =>
        string.Join(';', Split(path).Where(entry => Key(entry) != Key(folder)));
}
