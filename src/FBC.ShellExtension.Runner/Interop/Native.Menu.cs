using System.Runtime.InteropServices;

namespace FBC.ShellExtension.Runner.Interop;

// The taskbar menu: a popup menu of the runner's own, opened at the cursor (ADR-0043).
internal static unsafe partial class Native
{
    public const uint MF_STRING = 0x00000000;
    public const uint MF_GRAYED = 0x00000001;
    public const uint MF_POPUP = 0x00000010;
    public const uint MF_SEPARATOR = 0x00000800;
    public const uint MIIM_BITMAP = 0x00000080;

    public const uint TPM_LEFTALIGN = 0x0000;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_RIGHTALIGN = 0x0008;
    public const uint TPM_TOPALIGN = 0x0000;
    public const uint TPM_BOTTOMALIGN = 0x0020;
    public const uint TPM_NONOTIFY = 0x0080;
    public const uint TPM_RETURNCMD = 0x0100;

    public const uint WS_POPUP = 0x80000000;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;

    [StructLayout(LayoutKind.Sequential)]
    public struct MENUITEMINFOW
    {
        public uint cbSize;
        public uint fMask;
        public uint fType;
        public uint fState;
        public uint wID;
        public nint hSubMenu;
        public nint hbmpChecked;
        public nint hbmpUnchecked;
        public nuint dwItemData;
        public char* dwTypeData;
        public uint cch;
        public nint hbmpItem;
    }

    [LibraryImport("user32.dll", EntryPoint = "CreatePopupMenu", SetLastError = true)]
    public static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", EntryPoint = "DestroyMenu")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint hMenu);

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenuW(nint hMenu, uint uFlags, nuint uIDNewItem, string? lpNewItem);

    [LibraryImport("user32.dll", EntryPoint = "GetMenuItemCount")]
    public static partial int GetMenuItemCount(nint hMenu);

    [LibraryImport("user32.dll", EntryPoint = "SetMenuItemInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetMenuItemInfoW(nint hmenu, uint item, [MarshalAs(UnmanagedType.Bool)] bool fByPositon, MENUITEMINFOW* lpmii);

    /// <summary>With TPM_RETURNCMD: the chosen item's id, 0 when the menu was dismissed.</summary>
    [LibraryImport("user32.dll", EntryPoint = "TrackPopupMenuEx", SetLastError = true)]
    public static partial int TrackPopupMenuEx(nint hMenu, uint uFlags, int x, int y, nint hwnd, nint lptpm);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindowExW(nint hWndParent, nint hWndChildAfter, string? lpszClass, string? lpszWindow);

    [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hWnd);
}
