using System.Runtime.InteropServices;
using FBC.ShellExtension.Core;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>
/// The DLL's entry points. Each one catches everything: an exception that crosses an
/// [UnmanagedCallersOnly] boundary ends the process, and the process is Explorer.
/// </summary>
internal static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject")]
    private static int DllGetClassObject(Guid* rclsid, Guid* riid, nint* ppv)
    {
        if (ppv == null)
        {
            return E_POINTER;
        }

        *ppv = 0;
        try
        {
            if (rclsid == null || riid == null)
            {
                return E_INVALIDARG;
            }

            if (*rclsid != ProductInfo.ContextMenuClsid)
            {
                return CLASS_E_CLASSNOTAVAILABLE;
            }

            return ComObjects.QueryInterface(new ClassFactory(), *riid, ppv);
        }
        catch (Exception ex)
        {
            return Diagnostics.Fail(nameof(DllGetClassObject), ex);
        }
    }

    // A NativeAOT library cannot be unloaded, so the answer is always "not now" (ADR-0001).
    [UnmanagedCallersOnly(EntryPoint = "DllCanUnloadNow")]
    internal static int DllCanUnloadNow() => S_FALSE;

    [UnmanagedCallersOnly(EntryPoint = "DllRegisterServer")]
    private static int DllRegisterServer()
    {
        try
        {
            Registration.Register(Module.Path);
            return S_OK;
        }
        catch (Exception ex)
        {
            return Diagnostics.Fail(nameof(DllRegisterServer), ex, SELFREG_E_CLASS);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "DllUnregisterServer")]
    private static int DllUnregisterServer()
    {
        try
        {
            Registration.Unregister();
            return S_OK;
        }
        catch (Exception ex)
        {
            return Diagnostics.Fail(nameof(DllUnregisterServer), ex, SELFREG_E_CLASS);
        }
    }
}
