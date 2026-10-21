using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FBC.ShellExtension.Restarter.Interop;

// Every import names its Unicode entry point explicitly: LibraryImport does not probe for the
// A/W variants. Signatures follow winuser.h, shellapi.h, shlobj_core.h and restartmanager.h.
internal static unsafe partial class Native
{
    public const uint WM_COMMAND = 0x0111;
    public const uint GA_ROOT = 2;
    public const uint GW_HWNDNEXT = 2;
    public const uint SMTO_ABORTIFHUNG = 0x0002;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const uint WPF_RESTORETOMAXIMIZED = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const int STD_INPUT_HANDLE = -10;
    public const int STD_OUTPUT_HANDLE = -11;
    public const int STD_ERROR_HANDLE = -12;
    public const uint HANDLE_FLAG_INHERIT = 0x1;

    public const uint COINIT_APARTMENTTHREADED = 0x2;
    public const uint CLSCTX_ALL = 0x17;

    public const uint SEE_MASK_IDLIST = 0x00000004;
    public const uint SEE_MASK_NOASYNC = 0x00000100;
    public const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
    public const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;
    public const uint SBSP_SAMEBROWSER = 0x0001;
    public const uint SBSP_ABSOLUTE = 0x0000;

    public const ushort VT_I4 = 3;
    public const ushort VT_BSTR = 8;
    public const ushort DISPATCH_METHOD = 1;

    public const int ERROR_MORE_DATA = 234;
    public const int ERROR_FAIL_SHUTDOWN = 351;
    public const uint RmForceShutdown = 0x1;
    public const int CCH_RM_SESSION_KEY = 32;
    public const int RmExplorer = 4;

    // ---- user32 -----------------------------------------------------------------------------

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindow(string? className, string? windowName);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);

    [LibraryImport("user32.dll", EntryPoint = "GetShellWindow")]
    public static partial nint GetShellWindow();

    [LibraryImport("user32.dll", EntryPoint = "GetTopWindow")]
    public static partial nint GetTopWindow(nint parent);

    [LibraryImport("user32.dll", EntryPoint = "GetWindow")]
    public static partial nint GetWindow(nint hwnd, uint command);

    [LibraryImport("user32.dll", EntryPoint = "GetAncestor")]
    public static partial nint GetAncestor(nint hwnd, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW")]
    public static partial int GetClassName(nint hwnd, char* className, int maxCount);

    [LibraryImport("user32.dll", EntryPoint = "IsWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowPlacement")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowPlacement(nint hwnd, WINDOWPLACEMENT* placement);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowPlacement")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPlacement(nint hwnd, WINDOWPLACEMENT* placement);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint hwnd, RECT* rect);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    /// <summary>Windows 10 and later; an older user32 does not export it.</summary>
    [LibraryImport("user32.dll", EntryPoint = "IsWindowArranged")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowArranged(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    public static partial nint SendMessageTimeout(nint hwnd, uint message, nint wParam, nint lParam, uint flags, uint timeout, nint* result);

    // ---- ole32, oleaut32 --------------------------------------------------------------------

    [LibraryImport("ole32.dll", EntryPoint = "CoInitializeEx")]
    public static partial int CoInitializeEx(nint reserved, uint coInit);

    [LibraryImport("ole32.dll", EntryPoint = "CoUninitialize")]
    public static partial void CoUninitialize();

    [LibraryImport("ole32.dll", EntryPoint = "CoCreateInstance")]
    public static partial int CoCreateInstance(Guid* clsid, nint outer, uint context, Guid* iid, nint* instance);

    // ---- shell32 ----------------------------------------------------------------------------

    [LibraryImport("shell32.dll", EntryPoint = "ShellExecuteExW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShellExecuteEx(SHELLEXECUTEINFOW* info);

    [LibraryImport("shell32.dll", EntryPoint = "SHGetNameFromIDList")]
    public static partial int SHGetNameFromIDList(nint idList, uint sigdn, char** name);

    [LibraryImport("shell32.dll", EntryPoint = "ILGetSize")]
    public static partial uint ILGetSize(nint idList);

    // ---- kernel32 ---------------------------------------------------------------------------

    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle")]
    public static partial nint GetStdHandle(int standardHandle);

    [LibraryImport("kernel32.dll", EntryPoint = "SetHandleInformation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetHandleInformation(nint handle, uint mask, uint flags);

    [LibraryImport("kernel32.dll", EntryPoint = "GetProcessTimes", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetProcessTimes(SafeProcessHandle process, FILETIME* creation, FILETIME* exit, FILETIME* kernel, FILETIME* user);

    // ---- rstrtmgr ---------------------------------------------------------------------------

    [LibraryImport("rstrtmgr.dll", EntryPoint = "RmStartSession")]
    public static partial int RmStartSession(uint* session, uint flags, char* sessionKey);

    [LibraryImport("rstrtmgr.dll", EntryPoint = "RmRegisterResources")]
    public static partial int RmRegisterResources(uint session, uint fileCount, char** files, uint processCount, RM_UNIQUE_PROCESS* processes, uint serviceCount, char** services);

    [LibraryImport("rstrtmgr.dll", EntryPoint = "RmGetList")]
    public static partial int RmGetList(uint session, uint* needed, uint* count, RM_PROCESS_INFO* processes, uint* rebootReasons);

    [LibraryImport("rstrtmgr.dll", EntryPoint = "RmShutdown")]
    public static partial int RmShutdown(uint session, uint flags, nint statusCallback);

    [LibraryImport("rstrtmgr.dll", EntryPoint = "RmRestart")]
    public static partial int RmRestart(uint session, uint reserved, nint statusCallback);

    [LibraryImport("rstrtmgr.dll", EntryPoint = "RmEndSession")]
    public static partial int RmEndSession(uint session);
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

/// <summary>44 bytes: the <c>rcDevice</c> member in the documentation exists only on the Mac.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WINDOWPLACEMENT
{
    public uint length;
    public uint flags;
    public uint showCmd;
    public POINT ptMinPosition;
    public POINT ptMaxPosition;
    public RECT rcNormalPosition;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SHELLEXECUTEINFOW
{
    public uint cbSize;
    public uint fMask;
    public nint hwnd;
    public nint lpVerb;
    public nint lpFile;
    public nint lpParameters;
    public nint lpDirectory;
    public int nShow;
    public nint hInstApp;
    public nint lpIDList;
    public nint lpClass;
    public nint hkeyClass;
    public uint dwHotKey;
    public nint hIconOrMonitor;
    public nint hProcess;
}

/// <summary>A VARIANT as the x64 ABI lays it out: 24 bytes.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VARIANT
{
    public ushort vt;
    public ushort reserved1;
    public ushort reserved2;
    public ushort reserved3;
    public nint value;
    public nint value2;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DISPPARAMS
{
    public VARIANT* rgvarg;
    public int* rgdispidNamedArgs;
    public uint cArgs;
    public uint cNamedArgs;
}

/// <summary>Two DWORDs, so 4-byte aligned; a long here would move every field after it.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FILETIME
{
    public uint Low;
    public uint High;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RM_UNIQUE_PROCESS
{
    public int dwProcessId;
    public FILETIME ProcessStartTime;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct RM_PROCESS_INFO
{
    public RM_UNIQUE_PROCESS Process;
    public fixed char strAppName[256];
    public fixed char strServiceShortName[64];
    public int ApplicationType;
    public uint AppStatus;
    public uint TSSessionId;
    public int bRestartable;
}
