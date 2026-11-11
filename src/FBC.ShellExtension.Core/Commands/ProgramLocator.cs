using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Commands;

/// <summary>
/// Where the program a command starts is, for its icon (ADR-0041): found as the runner's start
/// finds it, without starting anything. A full path is taken as it is; a bare name, with ".exe"
/// added when it has none, is looked for in the folders the caller gives, in that order (the
/// tools folder, the runner's own, System32, Windows, then PATH). Pure: the caller says which
/// files exist.
/// </summary>
public static class ProgramLocator
{
    /// <summary>
    /// The program's full path; null when it cannot be found here: it names the selection, is a
    /// path relative to whatever folder Explorer is in, or is in none of the folders.
    /// </summary>
    public static string? Find(string fileName, Func<string, string?> getEnvironmentVariable, IReadOnlyList<string> folders, Func<string, bool> exists)
    {
        string name = fileName.Trim().Trim('"').Trim();
        if (name.Length == 0 || VariableExpander.UsesSelection(name))
        {
            return null;
        }

        // Only %NAME% is left to expand: the selection's names were ruled out above.
        var none = new SelectedItem(@"C:", ItemKind.Folder);
        name = VariableExpander.Expand(name, none, [none], getEnvironmentVariable);

        if (IsFullyQualified(name))
        {
            return exists(name) ? name : null;
        }

        if (name.Contains('\\') || name.Contains('/'))
        {
            return null;
        }

        string file = WindowsPath.GetExtension(name).Length == 0 ? name + ".exe" : name;
        foreach (string folder in folders)
        {
            if (folder.Length > 0)
            {
                string candidate = folder.TrimEnd('\\', '/') + "\\" + file;
                if (exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsFullyQualified(string path) =>
        (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '\\' or '/')
        || path.StartsWith(@"\\", StringComparison.Ordinal);
}
