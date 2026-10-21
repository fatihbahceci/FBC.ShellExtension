using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Restart;

namespace FBC.ShellExtension.Restarter;

internal static class Program
{
    private static int Main(string[] args)
    {
        RestartOptions options;
        try
        {
            options = RestartArguments.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Strings.RestarterUsage());
            return (int)RestartExitCode.Usage;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(Strings.RestarterUsage());
            return (int)RestartExitCode.Success;
        }

        // An elevated conductor may not get Explorer back from Restart Manager, and an
        // explorer.exe started from here would run the whole shell elevated (ADR-0008).
        if (Environment.IsPrivilegedProcess)
        {
            Console.Error.WriteLine(Strings.RestarterElevated());
            return (int)RestartExitCode.Elevated;
        }

        return (int)new ExplorerRestart(options).Run();
    }
}
