using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;
using FBC.ShellExtension.Core.Templates;

namespace FBC.ShellExtension.Manager;

internal static class Program
{
    private static string TemplatePath => Path.Combine(AppContext.BaseDirectory, ProductInfo.TemplateFileName);

    private static int Main(string[] args)
    {
        // The editor reads the output through a pipe; paths may hold any letter. A console keeps
        // its own code page, which is not changed for it.
        if (Console.IsOutputRedirected)
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false)) { AutoFlush = true });
        }

        if (Console.IsErrorRedirected)
        {
            Console.SetError(new StreamWriter(Console.OpenStandardError(), new System.Text.UTF8Encoding(false)) { AutoFlush = true });
        }

        Native.FailCriticalErrorsQuietly();

        // A bare program name (winget) is not looked up in whatever folder the caller was in.
        Environment.CurrentDirectory = Environment.SystemDirectory;
        TextWriter output = Console.Out;
        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "check";
        try
        {
            ManagerExitCode result = command switch
            {
                "check" => Check(output),
                "register" => Registration.Register(output),
                "unregister" => Registration.Unregister(output),
                "uninstall" => Uninstall(output, ownOnly: args.Skip(1).Any(arg => string.Equals(arg, "--own", StringComparison.OrdinalIgnoreCase))),
                "backup" => WithFile(args, file => Backup(file, output)),
                "restore" => WithFile(args, file => Restore(file, output)),
                "reset" => Reset(output),
                "ffmpeg" => FfmpegCommand(args, output),
                "help" or "--help" or "-h" or "/?" => Help(output),
                _ => Unknown(args[0]),
            };
            return (int)result;
        }
        catch (TemplateLoadException ex)
        {
            Console.Error.WriteLine(Strings.ManagerInvalidFile(TemplatePath, ex.Message));
            return (int)ManagerExitCode.Failed;
        }
        catch (Exception ex)
        {
            // The editor runs this at every start; whatever went wrong is reported, not crashed on.
            Console.Error.WriteLine(Strings.ManagerFailed(ex.Message));
            return (int)ManagerExitCode.Failed;
        }
    }

    /// <summary>What a portable copy leaves outside its folder, except the user's menu.</summary>
    /// <param name="ownOnly">
    /// The installer's uninstall: a registration that points at another copy (a portable folder)
    /// is that copy's, and stays (ADR-0022).
    /// </param>
    private static ManagerExitCode Uninstall(TextWriter output, bool ownOnly)
    {
        ManagerExitCode registration = Registration.Unregister(output, ownOnly);
        ManagerExitCode path = Ffmpeg.RemoveFromPath(output);
        return (ManagerExitCode)Math.Max((int)registration, (int)path);
    }

    private static ManagerExitCode FfmpegCommand(string[] args, TextWriter output)
    {
        string sub = string.Join(' ', args.Skip(1)).ToLowerInvariant();
        return sub switch
        {
            "" => Ffmpeg.Status(output),
            "install winget" => Ffmpeg.InstallWithWinget(output),
            "install download" => Ffmpeg.Download(output),
            "path add" => Ffmpeg.AddToPath(output),
            "path remove" => Ffmpeg.RemoveFromPath(output),
            _ => Unknown(string.Join(' ', args)),
        };
    }

    private static ManagerExitCode Help(TextWriter output)
    {
        output.WriteLine(Strings.ManagerUsage());
        return ManagerExitCode.Success;
    }

    private static ManagerExitCode Unknown(string command)
    {
        Console.Error.WriteLine(Strings.ManagerUnknownCommand(command));
        Console.Error.WriteLine();
        Console.Error.WriteLine(Strings.ManagerUsage());
        return ManagerExitCode.Usage;
    }

    private static ManagerExitCode WithFile(string[] args, Func<string, ManagerExitCode> action)
    {
        if (args.Length < 2 || args[1].Length == 0)
        {
            Console.Error.WriteLine(Strings.ManagerMissingFile(args[0]));
            return ManagerExitCode.Usage;
        }

        return action(args[1]);
    }

    /// <summary>The start of every run of the editor: the registration, then the menu against the template.</summary>
    private static ManagerExitCode Check(TextWriter output)
    {
        ManagerExitCode registration = Registration.Check(output);
        ManagerExitCode template = UpdateFromTemplate(output);
        return (ManagerExitCode)Math.Max((int)registration, (int)template);
    }

    private static ManagerExitCode UpdateFromTemplate(TextWriter output)
    {
        if (!File.Exists(TemplatePath))
        {
            output.WriteLine(Strings.ManagerTemplateMissing(TemplatePath));
            return ManagerExitCode.Attention;
        }

        MaintenanceResult result;
        try
        {
            result = ConfigMaintenance.UpdateFromTemplate(ConfigLocation.DefaultPath, TemplatePath, DateTime.Now, ProductInfo.Version);
        }
        catch (TemplateLoadException ex)
        {
            output.WriteLine(Strings.ManagerInvalidFile(TemplatePath, ex.Message));
            return ManagerExitCode.Attention;
        }
        catch (ConfigLoadException ex)
        {
            // An unreadable config is the editor's to sort out (ADR-0007); it is left as it is.
            output.WriteLine(Strings.ManagerInvalidFile(ConfigLocation.DefaultPath, ex.Message));
            return ManagerExitCode.Attention;
        }

        output.WriteLine(result.Action switch
        {
            TemplateAction.Created => Strings.ManagerTemplateCreated(),
            TemplateAction.Replaced => Strings.ManagerTemplateReplaced(),
            TemplateAction.Merged => Strings.ManagerTemplateMerged(string.Join(", ", result.AddedIds)),
            TemplateAction.ManagedByNewer => Strings.ManagerTemplateNewer(result.AppliedBy ?? "?"),
            TemplateAction.Skipped => Strings.ManagerTemplateSkipped(),
            _ => Strings.ManagerTemplateUpToDate(),
        });
        if (result.Backup is { } backup)
        {
            output.WriteLine(Strings.ManagerPreviousSaved(backup));
        }

        // A changed menu that got new items, or one left alone, is worth telling the user about.
        return result.Action is TemplateAction.Merged or TemplateAction.ManagedByNewer or TemplateAction.Skipped
            ? ManagerExitCode.Attention
            : ManagerExitCode.Success;
    }

    private static ManagerExitCode Backup(string file, TextWriter output)
    {
        if (!File.Exists(ConfigLocation.DefaultPath))
        {
            output.WriteLine(Strings.ManagerNoConfig());
            return ManagerExitCode.Attention;
        }

        ConfigMaintenance.Backup(ConfigLocation.DefaultPath, file);
        output.WriteLine(Strings.ManagerBackupSaved(Path.GetFullPath(file)));
        return ManagerExitCode.Success;
    }

    private static ManagerExitCode Restore(string file, TextWriter output)
    {
        string? backup;
        try
        {
            backup = ConfigMaintenance.Restore(ConfigLocation.DefaultPath, file, DateTime.Now);
        }
        catch (ConfigLoadException ex)
        {
            output.WriteLine(Strings.ManagerInvalidFile(file, ex.Message));
            return ManagerExitCode.Failed;
        }

        output.WriteLine(Strings.ManagerRestored(Path.GetFullPath(file)));
        if (backup is not null)
        {
            output.WriteLine(Strings.ManagerPreviousSaved(backup));
        }

        return ManagerExitCode.Success;
    }

    private static ManagerExitCode Reset(TextWriter output)
    {
        if (!File.Exists(TemplatePath))
        {
            output.WriteLine(Strings.ManagerTemplateMissing(TemplatePath));
            return ManagerExitCode.Failed;
        }

        string? backup = ConfigMaintenance.Reset(ConfigLocation.DefaultPath, TemplatePath, DateTime.Now, ProductInfo.Version);
        output.WriteLine(Strings.ManagerReset());
        if (backup is not null)
        {
            output.WriteLine(Strings.ManagerPreviousSaved(backup));
        }

        return ManagerExitCode.Success;
    }
}
