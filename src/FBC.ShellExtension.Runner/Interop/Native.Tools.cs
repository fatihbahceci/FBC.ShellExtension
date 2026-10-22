using System.Runtime.InteropServices;

namespace FBC.ShellExtension.Runner.Interop;

// Reading the clipboard, waking the text prompt's loop, copying and reading properties for the built-in tools (ADR-0017 … ADR-0019).
internal static unsafe partial class Native
{
    public const uint WM_NULL = 0x0000;

    /// <summary>Any process: the editor, started next, may bring its window to the front.</summary>
    public const uint ASFW_ANY = unchecked((uint)-1);

    [LibraryImport("user32.dll", EntryPoint = "AllowSetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllowSetForegroundWindow(uint processId);

    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostThreadMessageW(uint idThread, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "IsClipboardFormatAvailable")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll", EntryPoint = "GetClipboardData", SetLastError = true)]
    public static partial nint GetClipboardData(uint uFormat);

    public const uint COPY_FILE_FAIL_IF_EXISTS = 0x00000001;
    public const uint PROGRESS_CONTINUE = 0;
    public const uint PROGRESS_CANCEL = 1;

    /// <summary>Copies as Explorer does: alternate data streams, attributes, dates and encryption come along.</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "CopyFileExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CopyFileExW(string lpExistingFileName, string lpNewFileName,
        delegate* unmanaged<long, long, long, long, uint, uint, nint, nint, nint, uint> lpProgressRoutine,
        nint lpData, int* pbCancel, uint dwCopyFlags);

    public const uint GPS_BESTEFFORT = 0x40;
    public const uint COINIT_APARTMENTTHREADED = 0x2;
    public const ushort VT_BSTR = 8;
    public const ushort VT_LPWSTR = 31;

    [LibraryImport("ole32.dll", EntryPoint = "CoInitializeEx")]
    public static partial int CoInitializeEx(nint pvReserved, uint dwCoInit);

    [LibraryImport("ole32.dll", EntryPoint = "CoUninitialize")]
    public static partial void CoUninitialize();

    [LibraryImport("shell32.dll", EntryPoint = "SHGetPropertyStoreFromParsingName", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SHGetPropertyStoreFromParsingName(string pszPath, nint pbc, uint flags, Guid* riid, nint* ppv);

    [LibraryImport("ole32.dll", EntryPoint = "PropVariantClear")]
    public static partial int PropVariantClear(void* pvar);

    public const uint MB_SETFOREGROUND = 0x00010000;
    public const uint MB_TOPMOST = 0x00040000;

    public const uint ES_PASSWORD = 0x0020;
    public const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    public struct DATA_BLOB
    {
        public uint cbData;
        public byte* pbData;
    }

    [LibraryImport("crypt32.dll", EntryPoint = "CryptProtectData", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptProtectData(DATA_BLOB* pDataIn, string? szDataDescr, DATA_BLOB* pOptionalEntropy,
        nint pvReserved, nint pPromptStruct, uint dwFlags, DATA_BLOB* pDataOut);

    [LibraryImport("crypt32.dll", EntryPoint = "CryptUnprotectData", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptUnprotectData(DATA_BLOB* pDataIn, nint ppszDataDescr, DATA_BLOB* pOptionalEntropy,
        nint pvReserved, nint pPromptStruct, uint dwFlags, DATA_BLOB* pDataOut);

    [LibraryImport("kernel32.dll", EntryPoint = "LocalFree")]
    public static partial nint LocalFree(nint hMem);
}
