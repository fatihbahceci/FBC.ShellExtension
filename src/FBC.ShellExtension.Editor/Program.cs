using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Editor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
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
