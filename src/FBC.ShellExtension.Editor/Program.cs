using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Editor.Tools;

namespace FBC.ShellExtension.Editor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // A tool's window, handed over by the runner (ADR-0028); as many at once as are asked for.
        if (args is [JobFile.Argument, var jobPath])
        {
            ApplicationConfiguration.Initialize();
            ToolWindows.Run(jobPath);
            return;
        }

        // Started elevated by Unlock and delete's window, with the items to delete at restart (ADR-0031).
        if (args is [RebootDeletion.Argument, .. var paths] && paths.Length > 0)
        {
            ApplicationConfiguration.Initialize();
            Environment.ExitCode = RebootDeletion.Mark(paths);
            return;
        }

        // One editor per user session: a second start, from the menu item for example, brings
        // the open window forward instead of opening the same file twice (ADR-0007).
        Mutex instance;
        bool first;
        try
        {
            instance = new Mutex(initiallyOwned: true, @"Local\FBCTools.Editor", out first);
        }
        catch (UnauthorizedAccessException)
        {
            // An elevated editor made the mutex with a DACL this process cannot open.
            SingleInstance.ActivateExisting();
            return;
        }

        using (instance)
        {
            if (!first)
            {
                SingleInstance.ActivateExisting();
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(ConfigLocation.DefaultPath));
        }
    }
}
