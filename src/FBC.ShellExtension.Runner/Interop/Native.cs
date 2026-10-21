using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FBC.ShellExtension.Runner.Interop;

// Every import names its Unicode entry point explicitly: LibraryImport does not probe for the
// A/W variants, and a wrong name fails at the first call, not at publish time.
internal static unsafe partial class Native
{
    public const uint WM_CREATE = 0x0001;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_ACTIVATE = 0x0006;
    public const uint WM_CLOSE = 0x0010;
    public const uint WM_QUIT = 0x0012;
    public const uint WM_NEXTDLGCTL = 0x0028;
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_GETMINMAXINFO = 0x0024;
    public const uint WM_SETFONT = 0x0030;
    public const uint WM_NCDESTROY = 0x0082;
    public const uint WM_COMMAND = 0x0111;
    public const uint WM_TIMER = 0x0113;
    public const uint WM_CTLCOLORSTATIC = 0x0138;
    public const uint WM_DPICHANGED = 0x02E0;
    public const uint WM_APP = 0x8000;

    public const uint EM_GETSEL = 0x00B0;
    public const uint EM_SETSEL = 0x00B1;
    public const uint EM_LINESCROLL = 0x00B6;
    public const uint EM_LINEINDEX = 0x00BB;
    public const uint EM_LINEFROMCHAR = 0x00C9;
    public const uint EM_GETFIRSTVISIBLELINE = 0x00CE;
    public const uint EM_SCROLLCARET = 0x00B7;
    public const uint EM_REPLACESEL = 0x00C2;
    public const uint EM_SETLIMITTEXT = 0x00C5;

    public const uint CB_SETEDITSEL = 0x0142;
    public const uint CB_ADDSTRING = 0x0143;
    public const uint CB_SETCURSEL = 0x014E;

    public const uint PBM_SETPOS = 0x0402;
    public const uint PBM_SETRANGE32 = 0x0406;
    public const uint PBM_SETSTATE = 0x0410;
    public const nint PBST_ERROR = 2;
    public const nint PBST_PAUSED = 3;

    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    public const uint WS_CAPTION = 0x00C00000;
    public const uint WS_SYSMENU = 0x00080000;
    public const uint WS_CHILD = 0x40000000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_VSCROLL = 0x00200000;
    public const uint WS_TABSTOP = 0x00010000;
    public const uint WS_EX_DLGMODALFRAME = 0x00000001;
    public const uint WS_EX_CLIENTEDGE = 0x00000200;
    public const uint ES_MULTILINE = 0x0004;
    public const uint ES_AUTOVSCROLL = 0x0040;
    public const uint ES_AUTOHSCROLL = 0x0080;
    public const uint ES_NOHIDESEL = 0x0100;
    public const uint ES_READONLY = 0x0800;
    public const uint BS_PUSHBUTTON = 0x0000;
    public const uint BS_DEFPUSHBUTTON = 0x0001;
    public const uint SS_NOPREFIX = 0x0080;
    public const uint SS_EDITCONTROL = 0x2000;
    public const uint SS_ENDELLIPSIS = 0x4000;
    public const uint CBS_DROPDOWN = 0x0002;
    public const uint CBS_AUTOHSCROLL = 0x0040;

    public const int CW_USEDEFAULT = unchecked((int)0x80000000);
    public const int SW_SHOWNORMAL = 1;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const int COLOR_BTNFACE = 15;
    public const int COLOR_GRAYTEXT = 17;
    public const nint IDC_ARROW = 32512;
    public const nint IDI_APPLICATION = 32512;
    public const int IDOK = 1;
    public const int IDCANCEL = 2;
    public const int IDYES = 6;
    public const int BN_CLICKED = 0;
    public const uint MB_YESNO = 0x04;
    public const uint MB_ICONERROR = 0x10;
    public const uint MB_ICONQUESTION = 0x20;
    public const uint MB_ICONWARNING = 0x30;
    public const uint MB_ICONINFORMATION = 0x40;
    public const uint MB_DEFBUTTON2 = 0x100;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const uint SPI_GETICONTITLELOGFONT = 0x001F;
    public const uint ICC_PROGRESS_CLASS = 0x0020;
    public const uint ICC_STANDARD_CLASSES = 0x4000;
    public const int ERROR_CANCELLED = 1223;
    public const uint PM_REMOVE = 0x0001;
    public const nint VK_TAB = 0x09;
    public const nint VK_RETURN = 0x0D;
    public const nint VK_ESCAPE = 0x1B;
    public const int SB_VERT = 1;
    public const uint SIF_RANGE = 0x1;
    public const uint SIF_PAGE = 0x2;
    public const uint SIF_POS = 0x4;
    public const int MDT_EFFECTIVE_DPI = 0;
    public const int WA_INACTIVE = 0;
    public const uint DT_WORDBREAK = 0x0010;
    public const uint DT_SINGLELINE = 0x0020;
    public const uint DT_EXPANDTABS = 0x0040;
    public const uint DT_CALCRECT = 0x0400;
    public const uint DT_NOPREFIX = 0x0800;
    public const uint DT_EDITCONTROL = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public delegate* unmanaged<nint, uint, nint, nint, nint> lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public char* lpszMenuName;
        public char* lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LOGFONTW
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;
        public fixed char lfFaceName[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SCROLLINFO
    {
        public uint cbSize;
        public uint fMask;
        public int nMin;
        public int nMax;
        public uint nPage;
        public int nPos;
        public int nTrackPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INITCOMMONCONTROLSEX
    {
        public uint dwSize;
        public uint dwICC;
    }

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    public static partial ushort RegisterClassExW(WNDCLASSEXW* lpwcx);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowExW(uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetMessageW")]
    public static partial int GetMessageW(MSG* lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PeekMessageW(MSG* lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [LibraryImport("user32.dll", EntryPoint = "GetScrollInfo")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetScrollInfo(nint hwnd, int nBar, SCROLLINFO* lpsi);

    [LibraryImport("shcore.dll", EntryPoint = "GetDpiForMonitor")]
    public static partial int GetDpiForMonitor(nint hmonitor, int dpiType, uint* dpiX, uint* dpiY);

    [LibraryImport("user32.dll", EntryPoint = "IsDialogMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsDialogMessageW(nint hDlg, MSG* lpMsg);

    [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TranslateMessage(MSG* lpMsg);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    public static partial nint DispatchMessageW(MSG* lpMsg);

    [LibraryImport("user32.dll", EntryPoint = "PostQuitMessage")]
    public static partial void PostQuitMessage(int nExitCode);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    public static partial nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll", EntryPoint = "UpdateWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "DestroyWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "IsWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "EnableWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport("user32.dll", EntryPoint = "SetFocus")]
    public static partial nint SetFocus(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowTextW(nint hWnd, string lpString);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
    public static partial int GetWindowTextLengthW(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
    public static partial int GetWindowTextW(nint hWnd, char* lpString, int nMaxCount);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetClientRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(nint hWnd, RECT* lpRect);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint hWnd, RECT* lpRect);

    [LibraryImport("user32.dll", EntryPoint = "GetFocus")]
    public static partial nint GetFocus();

    [LibraryImport("user32.dll", EntryPoint = "IsChild")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsChild(nint hWndParent, nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetSysColor")]
    public static partial uint GetSysColor(int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "GetDC")]
    public static partial nint GetDC(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "ReleaseDC")]
    public static partial int ReleaseDC(nint hWnd, nint hDC);

    [LibraryImport("user32.dll", EntryPoint = "DrawTextW")]
    public static partial int DrawTextW(nint hdc, char* lpchText, int cchText, RECT* lprc, uint format);

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBoxW(nint hWnd, string lpText, string lpCaption, uint uType);

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    public static partial nint LoadCursorW(nint hInstance, nint lpCursorName);

    [LibraryImport("user32.dll", EntryPoint = "LoadIconW")]
    public static partial nint LoadIconW(nint hInstance, nint lpIconName);

    [LibraryImport("user32.dll", EntryPoint = "SetTimer")]
    public static partial nuint SetTimer(nint hWnd, nuint nIDEvent, uint uElapse, nint lpTimerFunc);

    [LibraryImport("user32.dll", EntryPoint = "KillTimer")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool KillTimer(nint hWnd, nuint uIDEvent);

    [LibraryImport("user32.dll", EntryPoint = "GetDpiForWindow")]
    public static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "AdjustWindowRectExForDpi")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AdjustWindowRectExForDpi(RECT* lpRect, uint dwStyle, [MarshalAs(UnmanagedType.Bool)] bool bMenu, uint dwExStyle, uint dpi);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoForDpi")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfoForDpi(uint uiAction, uint uiParam, void* pvParam, uint fWinIni, uint dpi);

    [LibraryImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(POINT* lpPoint);

    [LibraryImport("user32.dll", EntryPoint = "MonitorFromPoint")]
    public static partial nint MonitorFromPoint(POINT pt, uint dwFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(nint hMonitor, MONITORINFO* lpmi);

    [LibraryImport("gdi32.dll", EntryPoint = "CreateFontIndirectW")]
    public static partial nint CreateFontIndirectW(LOGFONTW* lplf);

    [LibraryImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint ho);

    [LibraryImport("gdi32.dll", EntryPoint = "SelectObject")]
    public static partial nint SelectObject(nint hdc, nint h);

    [LibraryImport("gdi32.dll", EntryPoint = "SetTextColor")]
    public static partial uint SetTextColor(nint hdc, uint color);

    [LibraryImport("comctl32.dll", EntryPoint = "InitCommonControlsEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InitCommonControlsEx(INITCOMMONCONTROLSEX* picce);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    public static partial nint GetModuleHandleW(nint lpModuleName);

    [LibraryImport("kernel32.dll", EntryPoint = "GetOEMCP")]
    public static partial uint GetOEMCP();

    [LibraryImport("kernel32.dll", EntryPoint = "GetACP")]
    public static partial uint GetACP();

    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint hObject);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", SetLastError = true)]
    public static partial SafeJobHandle CreateJobObjectW(nint lpJobAttributes, nint lpName);

    [LibraryImport("kernel32.dll", EntryPoint = "AssignProcessToJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AssignProcessToJobObject(SafeJobHandle hJob, SafeProcessHandle hProcess);

    [LibraryImport("kernel32.dll", EntryPoint = "TerminateJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateJobObject(SafeJobHandle hJob, uint uExitCode);

    [LibraryImport("kernel32.dll", EntryPoint = "GetNamedPipeClientProcessId", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);

    [LibraryImport("kernel32.dll", EntryPoint = "GetNamedPipeServerProcessId", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);

    public const uint CF_UNICODETEXT = 13;
    public const uint GMEM_MOVEABLE = 0x0002;
    public const nint HWND_MESSAGE = -3;
    public const uint UNIVERSAL_NAME_INFO_LEVEL = 1;
    public const int ERROR_MORE_DATA = 234;

    [LibraryImport("user32.dll", EntryPoint = "OpenClipboard", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll", EntryPoint = "CloseClipboard", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseClipboard();

    [LibraryImport("user32.dll", EntryPoint = "EmptyClipboard", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", EntryPoint = "SetClipboardData", SetLastError = true)]
    public static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalAlloc", SetLastError = true)]
    public static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalLock", SetLastError = true)]
    public static partial void* GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalUnlock")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalUnlock(nint hMem);

    [LibraryImport("kernel32.dll", EntryPoint = "GlobalFree")]
    public static partial nint GlobalFree(nint hMem);

    /// <returns>A Win32 error code; this function does not set the last error.</returns>
    [LibraryImport("mpr.dll", EntryPoint = "WNetGetUniversalNameW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int WNetGetUniversalNameW(string lpLocalPath, uint dwInfoLevel, void* lpBuffer, ref uint lpBufferSize);
}

internal sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeJobHandle()
        : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle() => Native.CloseHandle(handle);
}
