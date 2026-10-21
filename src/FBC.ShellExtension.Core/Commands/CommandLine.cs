using System.Text;

namespace FBC.ShellExtension.Core.Commands;

/// <summary>
/// Quoting for the rules the Microsoft C runtime and <c>CommandLineToArgvW</c> use to split a
/// command line. Under those rules a backslash run is literal unless a quote follows it, so
/// <c>"C:\"</c> is read as <c>C:"</c> plus whatever comes next; doubling the run fixes it.
/// cmd.exe parses differently and is not covered here.
/// </summary>
public static class CommandLine
{
    /// <summary>CreateProcessW takes at most 32,767 characters including the terminating null.</summary>
    public const int MaxLength = 32_766;

    /// <summary>
    /// Length of the command line .NET builds for CreateProcess: the file name in quotes, then
    /// a space and the arguments when there are any.
    /// </summary>
    public static int Length(string fileName, string arguments) =>
        fileName.Length + 2 + (arguments.Length > 0 ? arguments.Length + 1 : 0);

    /// <summary>Wraps the value in quotes, escaping embedded quotes and the trailing backslash run.</summary>
    public static string Quote(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        AppendQuoted(sb, value);
        return sb.ToString();
    }

    public static void AppendQuoted(StringBuilder sb, string value)
    {
        sb.Append('"');
        int backslashes = 0;
        foreach (char c in value)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1);
            }
            else
            {
                sb.Append('\\', backslashes);
            }

            backslashes = 0;
            sb.Append(c);
        }

        sb.Append('\\', backslashes * 2);
        sb.Append('"');
    }

    /// <summary>
    /// Doubles the trailing backslash run, for a value the caller has already put between
    /// quotes (<c>"{path}"</c> with <c>C:\</c>).
    /// </summary>
    public static string EscapeTrailingBackslashes(string value)
    {
        int run = value.Length - value.TrimEnd('\\').Length;
        return run == 0 ? value : value + new string('\\', run);
    }
}
