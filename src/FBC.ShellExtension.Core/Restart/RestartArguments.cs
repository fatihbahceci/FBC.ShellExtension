using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Restart;

/// <param name="Files">Files whose other users are listed after Explorer stops, such as the shell extension DLL.</param>
/// <param name="Restore">Open the saved windows and tabs again.</param>
/// <param name="Command">What runs while Explorer is stopped: the program, then its arguments. Empty for none.</param>
public sealed record RestartOptions(IReadOnlyList<string> Files, bool Restore, IReadOnlyList<string> Command, bool ShowHelp);

/// <summary>
/// <c>[--file &lt;path&gt;]... [--no-restore] [--help] [-- &lt;program&gt; [arguments]...]</c>.
/// Everything after <c>--</c> belongs to the command, unchanged.
/// </summary>
public static class RestartArguments
{
    public const string FileOption = "--file";
    public const string NoRestoreOption = "--no-restore";
    public const string HelpOption = "--help";
    public const string CommandSeparator = "--";

    /// <exception cref="ArgumentException">The arguments cannot be understood; the message says why.</exception>
    public static RestartOptions Parse(IReadOnlyList<string> args)
    {
        var files = new List<string>();
        bool restore = true;
        bool help = false;
        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case FileOption when i + 1 < args.Count && args[i + 1].Length > 0:
                    files.Add(args[++i]);
                    break;
                case FileOption:
                    throw new ArgumentException(Strings.RestarterMissingValue(FileOption));
                case NoRestoreOption:
                    restore = false;
                    break;
                case HelpOption or "-h" or "/?":
                    help = true;
                    break;
                case CommandSeparator when i + 1 < args.Count:
                    return new RestartOptions(files, restore, [.. args.Skip(i + 1)], help);
                case CommandSeparator:
                    throw new ArgumentException(Strings.RestarterMissingCommand());
                default:
                    throw new ArgumentException(Strings.RestarterUnknownOption(args[i]));
            }
        }

        return new RestartOptions(files, restore, [], help);
    }
}

/// <summary>What the process exit code tells the caller; dev-run and the editor act on it.</summary>
public enum RestartExitCode
{
    /// <summary>Explorer restarted, every window came back, the command (if any) succeeded.</summary>
    Success = 0,

    /// <summary>Explorer restarted, but something is reported: a window or tab that did not come back, files still in use.</summary>
    Warnings = 1,

    Usage = 2,

    /// <summary>Started elevated; nothing was done.</summary>
    Elevated = 3,

    /// <summary>Explorer could not be stopped; the command did not run.</summary>
    StopFailed = 4,

    /// <summary>The command failed or could not start; Explorer was started again.</summary>
    CommandFailed = 5,
}
