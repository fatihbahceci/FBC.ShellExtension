using System.Text;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Commands;

/// <summary>
/// Replaces <c>{path}</c>, <c>{dir}</c>, <c>{name}</c>, <c>{nameNoExt}</c>, <c>{ext}</c>, <c>{pathNoExt}</c>,
/// <c>{allPaths}</c> and <c>%ENV%</c> in one pass over the template.
/// </summary>
/// <remarks>
/// <para>Inserted values are never scanned again, so a file named <c>100%PATH%.txt</c> stays as it is.</para>
/// <para>Values are inserted bare; the template supplies the quotes (<c>"{path}"</c>).
/// <c>{allPaths}</c> is the exception: every path is quoted and they are joined with spaces.</para>
/// <para>A value followed by a quote in the template gets its trailing backslashes doubled,
/// so <c>"{path}"</c> on <c>C:\</c> still reaches the program as <c>C:\</c>.</para>
/// <para>Anything else in braces, such as a PowerShell script block, and any undefined
/// <c>%NAME%</c> are left as written.</para>
/// </remarks>
public static class VariableExpander
{
    public static string Expand(
        string template,
        SelectedItem current,
        IReadOnlyList<SelectedItem> all,
        Func<string, string?> getEnvironmentVariable)
    {
        var sb = new StringBuilder(template.Length + 64);
        int i = 0;
        while (i < template.Length)
        {
            char c = template[i];
            string? value = null;
            int close = -1;
            if (c == '{')
            {
                close = template.IndexOf('}', i + 1);
                if (close > i + 1)
                {
                    value = GetPlaceholder(template.AsSpan(i + 1, close - i - 1), current, all);
                }
            }
            else if (c == '%')
            {
                close = template.IndexOf('%', i + 1);
                if (close > i + 1)
                {
                    value = getEnvironmentVariable(template[(i + 1)..close]);
                }
            }

            if (value is null)
            {
                sb.Append(c);
                i++;
                continue;
            }

            bool quoteFollows = close + 1 < template.Length && template[close + 1] == '"';
            sb.Append(quoteFollows ? CommandLine.EscapeTrailingBackslashes(value) : value);
            i = close + 1;
        }

        return sb.ToString();
    }

    public static string Expand(string template, SelectedItem current, IReadOnlyList<SelectedItem> all) =>
        Expand(template, current, all, Environment.GetEnvironmentVariable);

    private static string? GetPlaceholder(ReadOnlySpan<char> name, SelectedItem current, IReadOnlyList<SelectedItem> all)
    {
        if (name.Equals("path", StringComparison.OrdinalIgnoreCase))
        {
            return current.Path;
        }

        if (name.Equals("dir", StringComparison.OrdinalIgnoreCase))
        {
            return current.Directory;
        }

        if (name.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            return current.Name;
        }

        if (name.Equals("nameNoExt", StringComparison.OrdinalIgnoreCase))
        {
            return current.NameWithoutExtension;
        }

        if (name.Equals("ext", StringComparison.OrdinalIgnoreCase))
        {
            return current.Extension;
        }

        if (name.Equals("pathNoExt", StringComparison.OrdinalIgnoreCase))
        {
            return current.PathWithoutExtension;
        }

        if (name.Equals("allPaths", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            foreach (SelectedItem item in all)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                CommandLine.AppendQuoted(sb, item.Path);
            }

            return sb.ToString();
        }

        return null;
    }
}
