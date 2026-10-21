using System.Runtime.InteropServices;
using static FBC.ShellExtension.Restarter.Interop.Native;

namespace FBC.ShellExtension.Restarter.Interop;

/// <summary>
/// The few shell COM calls the restarter makes, through the interfaces' vtables. The objects
/// live in Explorer, so every call crosses into its process; they are made on one STA thread
/// and released there, never from a finalizer. Slot numbers count from IUnknown's three:
/// <list type="bullet">
/// <item>IShellWindows : IDispatch — get_Count 7, Item 8 (exdisp.h)</item>
/// <item>IDispatch — GetIDsOfNames 5, Invoke 6 (oaidl.h)</item>
/// <item>IServiceProvider — QueryService 3 (servprov.h)</item>
/// <item>IShellBrowser : IOleWindow — GetWindow 3, BrowseObject 11, QueryActiveShellView 15 (shobjidl_core.h)</item>
/// <item>IFolderView — GetFolder 5; IPersistFolder2 — GetCurFolder 5</item>
/// </list>
/// </summary>
internal static unsafe class ShellCom
{
    public static readonly Guid CLSID_ShellWindows = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    public static readonly Guid IID_IShellWindows = new("85CB6900-4D95-11CF-960C-0080C7F4EE85");
    public static readonly Guid IID_IServiceProvider = new("6D5140C1-7436-11CE-8034-00AA006009FA");
    public static readonly Guid IID_IShellBrowser = new("000214E2-0000-0000-C000-000000000046");
    public static readonly Guid SID_STopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
    public static readonly Guid IID_IFolderView = new("CDE725B0-CCC9-4519-917E-325D72FAB4CE");
    public static readonly Guid IID_IPersistFolder2 = new("1AC3D9F0-175C-11D1-95BE-00609797EA4F");

    private static void* Slot(nint instance, int index) => (*(void***)instance)[index];

    public static void Release(nint instance)
    {
        if (instance != 0)
        {
            Marshal.Release(instance);
        }
    }

    public static nint CreateShellWindows()
    {
        Guid clsid = CLSID_ShellWindows;
        Guid iid = IID_IShellWindows;
        nint instance;
        Marshal.ThrowExceptionForHR(CoCreateInstance(&clsid, 0, CLSCTX_ALL, &iid, &instance));
        return instance;
    }

    public static int GetCount(nint shellWindows)
    {
        int count;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int*, int>)Slot(shellWindows, 7))(shellWindows, &count));
        return count;
    }

    /// <returns>The window's automation object (IDispatch), or 0 when the index is empty.</returns>
    public static nint Item(nint shellWindows, int index)
    {
        var variant = new VARIANT { vt = VT_I4, value = index };
        nint item = 0;
        int hr = ((delegate* unmanaged[Stdcall]<nint, VARIANT, nint*, int>)Slot(shellWindows, 8))(shellWindows, variant, &item);
        return hr == 0 ? item : 0;
    }

    public static nint QueryInterface(nint instance, Guid iid) =>
        Marshal.QueryInterface(instance, in iid, out nint result) == 0 ? result : 0;

    public static nint QueryService(nint serviceProvider, Guid service, Guid iid)
    {
        nint result = 0;
        int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, nint*, int>)Slot(serviceProvider, 3))(serviceProvider, &service, &iid, &result);
        return hr == 0 ? result : 0;
    }

    public static nint GetWindow(nint oleWindow)
    {
        nint hwnd = 0;
        int hr = ((delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(oleWindow, 3))(oleWindow, &hwnd);
        return hr == 0 ? hwnd : 0;
    }

    public static int BrowseObject(nint shellBrowser, nint idList, uint flags) =>
        ((delegate* unmanaged[Stdcall]<nint, nint, uint, int>)Slot(shellBrowser, 11))(shellBrowser, idList, flags);

    public static nint QueryActiveShellView(nint shellBrowser)
    {
        nint view = 0;
        int hr = ((delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(shellBrowser, 15))(shellBrowser, &view);
        return hr == 0 ? view : 0;
    }

    public static nint GetFolder(nint folderView, Guid iid)
    {
        nint folder = 0;
        int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Slot(folderView, 5))(folderView, &iid, &folder);
        return hr == 0 ? folder : 0;
    }

    /// <returns>The folder's absolute ID list, owned by the caller (CoTaskMemFree), or 0.</returns>
    public static nint GetCurFolder(nint persistFolder2)
    {
        nint idList = 0;
        int hr = ((delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(persistFolder2, 5))(persistFolder2, &idList);
        return hr == 0 ? idList : 0;
    }

    /// <summary>Late-bound <c>IWebBrowser2.Navigate2(url)</c>, the route open-source tab tools use.</summary>
    public static int Navigate(nint dispatch, string url)
    {
        int dispId;
        Guid empty = Guid.Empty;
        fixed (char* name = "Navigate2")
        {
            char* names = name;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, char**, uint, uint, int*, int>)Slot(dispatch, 5))(dispatch, &empty, &names, 1, 0x0400, &dispId);
            if (hr != 0)
            {
                return hr;
            }
        }

        nint bstr = Marshal.StringToBSTR(url);
        try
        {
            var argument = new VARIANT { vt = VT_BSTR, value = bstr };
            var parameters = new DISPPARAMS { rgvarg = &argument, cArgs = 1 };
            return ((delegate* unmanaged[Stdcall]<nint, int, Guid*, uint, ushort, DISPPARAMS*, VARIANT*, nint, uint*, int>)Slot(dispatch, 6))(
                dispatch, dispId, &empty, 0x0400, DISPATCH_METHOD, &parameters, null, 0, null);
        }
        finally
        {
            Marshal.FreeBSTR(bstr);
        }
    }

    /// <returns>The parsing name of an ID list, such as <c>C:\Users</c> or <c>::{20D04FE0-…}</c>; empty when it has none.</returns>
    public static string ParsingName(nint idList)
    {
        char* name;
        if (SHGetNameFromIDList(idList, SIGDN_DESKTOPABSOLUTEPARSING, &name) != 0)
        {
            return "";
        }

        try
        {
            return new string(name);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)name);
        }
    }

    public static byte[] IdListBytes(nint idList)
    {
        var bytes = new byte[ILGetSize(idList)];
        Marshal.Copy(idList, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>A copy of the bytes in task memory, for calls that take an ID list pointer.</summary>
    public static nint IdListFromBytes(byte[] bytes)
    {
        nint idList = Marshal.AllocCoTaskMem(bytes.Length);
        Marshal.Copy(bytes, 0, idList, bytes.Length);
        return idList;
    }
}
