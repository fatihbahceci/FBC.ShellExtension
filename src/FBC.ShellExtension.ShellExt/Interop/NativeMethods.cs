using System.Runtime.InteropServices;

namespace FBC.ShellExtension.ShellExt.Interop;

internal static unsafe partial class NativeMethods
{
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int E_POINTER = unchecked((int)0x80004003);
    public const int E_FAIL = unchecked((int)0x80004005);
    public const int E_INVALIDARG = unchecked((int)0x80070057);
    public const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
    public const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
    public const int SELFREG_E_CLASS = unchecked((int)0x80040201);

    public const uint CMF_DEFAULTONLY = 0x00000001;

    public const uint MF_STRING = 0x00000000;
    public const uint MF_GRAYED = 0x00000001;
    public const uint MF_POPUP = 0x00000010;
    public const uint MF_BYPOSITION = 0x00000400;
    public const uint MF_SEPARATOR = 0x00000800;

    public const ushort CF_HDROP = 15;
    public const uint DVASPECT_CONTENT = 1;
    public const uint TYMED_HGLOBAL = 1;

    public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    public const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

    public const uint GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x2;
    public const uint GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x4;

    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0;

    [LibraryImport("shell32.dll", EntryPoint = "DragQueryFileW")]
    public static partial uint DragQueryFile(nint hDrop, uint iFile, char* lpszFile, uint cch);

    [LibraryImport("shell32.dll", EntryPoint = "SHGetPathFromIDListEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SHGetPathFromIDListEx(nint pidl, char* pszPath, uint cchPath, int uOpts);

    [LibraryImport("shell32.dll")]
    public static partial void SHChangeNotify(int wEventId, uint uFlags, nint dwItem1, nint dwItem2);

    [LibraryImport("ole32.dll")]
    public static partial void ReleaseStgMedium(STGMEDIUM* pmedium);

    [LibraryImport("kernel32.dll", EntryPoint = "GetFileAttributesW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint GetFileAttributes(string lpFileName);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateProcess(
        char* lpApplicationName,
        char* lpCommandLine,
        nint lpProcessAttributes,
        nint lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        char* lpCurrentDirectory,
        STARTUPINFOW* lpStartupInfo,
        PROCESS_INFORMATION* lpProcessInformation);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint hObject);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetModuleHandleEx(uint dwFlags, nint lpModuleName, out nint phModule);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", SetLastError = true)]
    public static partial uint GetModuleFileName(nint hModule, char* lpFilename, uint nSize);

    [LibraryImport("kernel32.dll", EntryPoint = "OutputDebugStringW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void OutputDebugString(string lpOutputString);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint hMenu);

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenu(nint hMenu, uint uFlags, nuint uIDNewItem, string? lpNewItem);

    [LibraryImport("user32.dll", EntryPoint = "InsertMenuW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InsertMenu(nint hMenu, uint uPosition, uint uFlags, nuint uIDNewItem, string? lpNewItem);
}
