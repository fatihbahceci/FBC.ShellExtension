namespace FBC.ShellExtension.Core.Selection;

/// <summary>
/// The few path operations the placeholders need, with Windows semantics regardless of the
/// OS the code runs on. <see cref="System.IO.Path"/> treats '\' as an ordinary character on
/// Linux, which would make the Core tests disagree with Explorer.
/// </summary>
internal static class WindowsPath
{
    private static bool IsSeparator(char c) => c is '\\' or '/';

    private static string TrimEndSeparators(string path) => path.TrimEnd('\\', '/');

    private static bool IsDriveSpecifier(ReadOnlySpan<char> path) =>
        path.Length == 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    /// <summary><c>C:\</c>, <c>C:</c> or a share root such as <c>\\server\share</c>.</summary>
    public static bool IsRoot(string path)
    {
        string trimmed = TrimEndSeparators(path);
        if (IsDriveSpecifier(trimmed))
        {
            return true;
        }

        if (trimmed.Length > 2 && IsSeparator(trimmed[0]) && IsSeparator(trimmed[1]))
        {
            // \\server\share has exactly one separator after the server name.
            int separators = 0;
            foreach (char c in trimmed.AsSpan(2))
            {
                if (IsSeparator(c))
                {
                    separators++;
                }
            }

            return separators == 1;
        }

        return false;
    }

    public static string GetFileName(string path)
    {
        string trimmed = TrimEndSeparators(path);
        return trimmed[(trimmed.LastIndexOfAny(['\\', '/']) + 1)..];
    }

    /// <summary>The parent folder; a bare drive specifier is completed to its root (<c>C:\</c>).</summary>
    public static string GetDirectoryName(string path)
    {
        string trimmed = TrimEndSeparators(path);
        int last = trimmed.LastIndexOfAny(['\\', '/']);
        if (last < 0)
        {
            return "";
        }

        string parent = trimmed[..last];
        return IsDriveSpecifier(parent) ? parent + "\\" : parent;
    }

    /// <summary>Whether <paramref name="path"/> is <paramref name="folder"/> or lies inside it, regardless of case.</summary>
    public static bool IsSameOrInside(string path, string folder)
    {
        string inner = TrimEndSeparators(path);
        string outer = TrimEndSeparators(folder);
        return string.Equals(inner, outer, StringComparison.OrdinalIgnoreCase)
            || (inner.Length > outer.Length && inner.StartsWith(outer, StringComparison.OrdinalIgnoreCase) && IsSeparator(inner[outer.Length]));
    }

    /// <summary>From the last dot of the name, as <see cref="System.IO.Path.GetExtension(string)"/> does.</summary>
    public static string GetExtension(string name)
    {
        int dot = name.LastIndexOf('.');
        return dot < 0 || dot == name.Length - 1 ? "" : name[dot..];
    }

    private static readonly string[] s_deviceNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    ];

    /// <summary>
    /// A name Windows takes for a file or folder: none of <c>&lt;&gt;:"/\|?*</c> or control
    /// characters, not ending in a dot or a space, and not a device name such as CON or COM1,
    /// with or without an extension.
    /// </summary>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.EndsWith('.') || name.EndsWith(' ')
            || name.IndexOfAny(['<', '>', ':', '"', '/', '\\', '|', '?', '*']) >= 0 || name.Any(char.IsControl))
        {
            return false;
        }

        string stem = name.Split('.')[0].TrimEnd(' ');
        return !s_deviceNames.Contains(stem, StringComparer.OrdinalIgnoreCase);
    }
}
