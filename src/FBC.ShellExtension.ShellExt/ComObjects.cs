using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using FBC.ShellExtension.ShellExt.Interop;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

internal static unsafe class ComObjects
{
    private static readonly StrategyBasedComWrappers s_wrappers = new();

    /// <summary>Hands out <paramref name="instance"/> as the requested interface, with one reference for the caller.</summary>
    public static int QueryInterface(object instance, Guid iid, nint* ppv)
    {
        nint unknown = s_wrappers.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);
        try
        {
            return Marshal.QueryInterface(unknown, iid, out *ppv);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }
}

[GeneratedComClass]
internal sealed unsafe partial class ClassFactory : IClassFactory
{
    public int CreateInstance(nint pUnkOuter, Guid* riid, nint* ppvObject)
    {
        if (ppvObject == null)
        {
            return E_POINTER;
        }

        *ppvObject = 0;
        try
        {
            if (riid == null)
            {
                return E_INVALIDARG;
            }

            if (pUnkOuter != 0)
            {
                return CLASS_E_NOAGGREGATION;
            }

            return ComObjects.QueryInterface(new ContextMenuHandler(), *riid, ppvObject);
        }
        catch (Exception ex)
        {
            return Diagnostics.Fail(nameof(CreateInstance), ex);
        }
    }

    // The DLL is never unloaded, so there is no lock count to keep.
    public int LockServer(int fLock) => S_OK;
}
