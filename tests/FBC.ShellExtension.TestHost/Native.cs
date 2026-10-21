using System.Runtime.InteropServices;

namespace FBC.ShellExtension.TestHost;

internal static unsafe partial class Native
{
    public static readonly Guid IID_IClassFactory = new("00000001-0000-0000-C000-000000000046");
    public static readonly Guid IID_IShellExtInit = new("000214E8-0000-0000-C000-000000000046");
    public static readonly Guid IID_IContextMenu = new("000214E4-0000-0000-C000-000000000046");
    public static readonly Guid IID_IShellFolder = new("000214E6-0000-0000-C000-000000000046");
    public static readonly Guid IID_IDataObject = new("0000010E-0000-0000-C000-000000000046");

    public const uint CMF_NORMAL = 0;
    public const int SW_SHOWNORMAL = 1;
    public const uint MF_BYPOSITION = 0x400;
    public const uint MF_GRAYED = 0x1;
    public const uint MF_SEPARATOR = 0x800;

    [StructLayout(LayoutKind.Sequential)]
    public struct CMINVOKECOMMANDINFO
    {
        public uint cbSize;
        public uint fMask;
        public nint hwnd;
        public nint lpVerb;
        public nint lpParameters;
        public nint lpDirectory;
        public int nShow;
        public uint dwHotKey;
        public nint hIcon;
    }

    /// <summary>The function pointer in a COM object's vtable at <paramref name="slot"/>.</summary>
    public static void* Slot(nint comObject, int slot) => (*(void***)comObject)[slot];

    public static void Release(nint comObject) => Marshal.Release(comObject);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SHParseDisplayName(string pszName, nint pbc, out nint ppidl, uint sfgaoIn, out uint psfgaoOut);

    [LibraryImport("shell32.dll")]
    public static partial int SHBindToParent(nint pidl, Guid* riid, nint* ppv, nint* ppidlLast);

    [LibraryImport("shell32.dll")]
    public static partial nint ILClone(nint pidl);

    [LibraryImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ILRemoveLastID(nint pidl);

    [LibraryImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ILIsEqual(nint pidl1, nint pidl2);

    [LibraryImport("shell32.dll")]
    public static partial void ILFree(nint pidl);

    [LibraryImport("user32.dll")]
    public static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint hMenu);

    [LibraryImport("user32.dll")]
    public static partial int GetMenuItemCount(nint hMenu);

    [LibraryImport("user32.dll")]
    public static partial uint GetMenuItemID(nint hMenu, int nPos);

    [LibraryImport("user32.dll")]
    public static partial uint GetMenuState(nint hMenu, uint uId, uint uFlags);

    [LibraryImport("user32.dll")]
    public static partial nint GetSubMenu(nint hMenu, int nPos);

    [LibraryImport("user32.dll", EntryPoint = "GetMenuStringW")]
    public static partial int GetMenuString(nint hMenu, uint uIDItem, char* lpString, int cchMax, uint flags);
}

internal sealed class HResultException(string message) : Exception(message);
