using System.Diagnostics;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;
using Microsoft.Win32;

namespace FBC.ShellExtension.Manager;

/// <summary>
/// Which copy of the shell extension Explorer uses. A portable folder that was moved, or a
/// second copy, leaves the registration pointing elsewhere; the check points it back here
/// unless it points at a newer version.
/// </summary>
internal static class Registration
{
    public static string OwnDll => Path.Combine(AppContext.BaseDirectory, ProductInfo.ShellExtFileName);

    public static ManagerExitCode Check(TextWriter output)
    {
        string own = OwnDll;
        if (!File.Exists(own))
        {
            output.WriteLine(Strings.ManagerDllMissing(own));
            return ManagerExitCode.Failed;
        }

        string? registered = RegisteredDll();
        if (registered is not null && SamePath(registered, own) && HandlersPresent())
        {
            output.WriteLine(Strings.ManagerRegistrationOk(own));
            return ManagerExitCode.Success;
        }

        if (registered is not null && !SamePath(registered, own) && File.Exists(registered)
            && FileVersion(registered) is { } theirs && FileVersion(own) is { } ours && theirs > ours)
        {
            output.WriteLine(Strings.ManagerRegisteredElsewhere(registered, theirs.ToString()));
            return ManagerExitCode.Attention;
        }

        return Register(output);
    }

    public static ManagerExitCode Register(TextWriter output)
    {
        string own = OwnDll;
        if (!File.Exists(own))
        {
            output.WriteLine(Strings.ManagerDllMissing(own));
            return ManagerExitCode.Failed;
        }

        int exitCode = RunRegsvr32($"/s \"{own}\"");
        output.WriteLine(exitCode == 0 ? Strings.ManagerRegistered(own) : Strings.ManagerRegisterFailed(own, exitCode));
        return exitCode == 0 ? ManagerExitCode.Success : ManagerExitCode.Failed;
    }

    /// <param name="ownOnly">
    /// Leave a registration that points at another copy as it is, while that copy is there; one
    /// that points at a deleted file is removed.
    /// </param>
    public static ManagerExitCode Unregister(TextWriter output, bool ownOnly = false)
    {
        string? registered = RegisteredDll();
        if (ownOnly && registered is not null && File.Exists(registered) && !SamePath(registered, OwnDll))
        {
            output.WriteLine(Strings.ManagerOtherCopyKept(registered));
            return ManagerExitCode.Success;
        }

        // The registered copy unregisters itself; it may be another folder's.
        string target = registered is not null && File.Exists(registered) ? registered : OwnDll;
        int exitCode = RunRegsvr32($"/u /s \"{target}\"");
        if (exitCode != 0)
        {
            output.WriteLine(Strings.ManagerRegisterFailed(target, exitCode));
            return ManagerExitCode.Failed;
        }

        output.WriteLine(Strings.ManagerUnregistered());
        return ManagerExitCode.Success;
    }

    private static string? RegisteredDll()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistrationKeys.InprocServerKey);
        return key?.GetValue(null) as string;
    }

    private static bool HandlersPresent() =>
        RegistrationKeys.HandlerClasses.All(handlerClass =>
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistrationKeys.HandlerKey(handlerClass));
            return string.Equals(key?.GetValue(null) as string, RegistrationKeys.Clsid, StringComparison.OrdinalIgnoreCase);
        });

    private static bool SamePath(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);

    private static Version? FileVersion(string path) =>
        Version.TryParse(FileVersionInfo.GetVersionInfo(path).FileVersion, out Version? version) ? version : null;

    private static int RunRegsvr32(string arguments)
    {
        string regsvr32 = Path.Combine(Environment.SystemDirectory, "regsvr32.exe");
        using Process? process = Process.Start(new ProcessStartInfo(regsvr32, arguments) { UseShellExecute = false });
        if (process is null)
        {
            return -1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
