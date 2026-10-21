using FBC.ShellExtension.Core.Tools;
using Microsoft.Win32;

namespace FBC.ShellExtension.Manager;

/// <summary>The PATH as the registry has it now, the same way the runner builds it (ADR-0011).</summary>
internal static class RegistryPath
{
    private const string MachineKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
    private const string UserKey = "Environment";

    public static string Current(string? toolsFolder)
    {
        using RegistryKey? machine = Registry.LocalMachine.OpenSubKey(MachineKey);
        using RegistryKey? user = Registry.CurrentUser.OpenSubKey(UserKey);
        return SearchPath.Merge(machine?.GetValue("Path") as string, user?.GetValue("Path") as string,
            Environment.GetEnvironmentVariable("PATH"), toolsFolder);
    }

    /// <summary>The user's own PATH value, unexpanded, as it is stored.</summary>
    public static string? UserValue()
    {
        using RegistryKey? user = Registry.CurrentUser.OpenSubKey(UserKey);
        return user?.GetValue("Path", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    }

    /// <summary>Writes the user's PATH, keeping it expandable, and tells running programs.</summary>
    public static void SetUserValue(string value)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UserKey))
        {
            key.SetValue("Path", value, RegistryValueKind.ExpandString);
        }

        Native.BroadcastEnvironmentChange();
    }
}
