using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Setup;
using Microsoft.Win32;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>
/// Per-user registration under HKCU\Software\Classes, so regsvr32 needs no elevation
/// (ADR-0005). Explorer reads HKCR, which merges these keys with HKLM's.
/// </summary>
internal static class Registration
{
    public static void Register(string dllPath)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistrationKeys.ClsidKey))
        {
            key.SetValue("", $"{ProductInfo.Name} context menu");
        }

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistrationKeys.InprocServerKey))
        {
            key.SetValue("", dllPath);
            key.SetValue("ThreadingModel", "Apartment");
        }

        foreach (string handlerClass in RegistrationKeys.HandlerClasses)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistrationKeys.HandlerKey(handlerClass));
            key.SetValue("", RegistrationKeys.Clsid);
        }

        NotifyShell();
    }

    public static void Unregister()
    {
        foreach (string handlerClass in RegistrationKeys.HandlerClasses)
        {
            Registry.CurrentUser.DeleteSubKeyTree(RegistrationKeys.HandlerKey(handlerClass), throwOnMissingSubKey: false);
        }

        Registry.CurrentUser.DeleteSubKeyTree(RegistrationKeys.ClsidKey, throwOnMissingSubKey: false);
        NotifyShell();
    }

    private static void NotifyShell() => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
}
