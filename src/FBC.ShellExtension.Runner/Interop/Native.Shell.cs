using System.Runtime.InteropServices;

namespace FBC.ShellExtension.Runner.Interop;

// Windows' folder picker, copy engine, icon picker, folder icons and links, for the folder-picker
// tools (ADR-0026). Values and vtable slots from the Windows SDK headers (10.0.16299), checked
// against the mingw-w64 and Wine copies.
internal static unsafe partial class Native
{
    public const uint CLSCTX_INPROC_SERVER = 0x1;
    public const uint COINIT_DISABLE_OLE1DDE = 0x4;

    // IFileDialog options.
    public const uint FOS_NOCHANGEDIR = 0x8;
    public const uint FOS_PICKFOLDERS = 0x20;
    public const uint FOS_FORCEFILESYSTEM = 0x40;
    public const uint FOS_PATHMUSTEXIST = 0x800;

    public const int SIGDN_FILESYSPATH = unchecked((int)0x80058000);

    // IFileOperation: undo as Explorer's own operations, no question before making a folder; the
    // progress, conflict and error dialogs stay (no FOF_SILENT, FOF_NOCONFIRMATION, FOF_NOERRORUI).
    public const uint FOF_ALLOWUNDO = 0x0040;
    public const uint FOF_NOCONFIRMMKDIR = 0x0200;
    public const uint FOFX_ADDUNDORECORD = 0x20000000;

    // HRESULTs a cancelled operation or dialog comes back with.
    public const int HRESULT_CANCELLED = unchecked((int)0x800704C7);
    public const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);

    // SHGetSetFolderCustomSettings.
    public const uint FCSM_ICONFILE = 0x10;
    public const uint FCS_READ = 0x1;
    public const uint FCS_FORCEWRITE = 0x2;

    // SHChangeNotify.
    public const int SHCNE_UPDATEDIR = 0x00001000;
    public const int SHCNE_UPDATEITEM = 0x00002000;
    public const uint SHCNF_PATHW = 0x0005;
    public const uint SHCNF_FLUSH = 0x1000;

    // Junctions.
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    public const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
    public const uint DRIVE_REMOTE = 4;
    public const int ERROR_PRIVILEGE_NOT_HELD = 1314;

    [LibraryImport("ole32.dll", EntryPoint = "CoCreateInstance")]
    public static partial int CoCreateInstance(Guid* rclsid, nint pUnkOuter, uint dwClsContext, Guid* riid, nint* ppv);

    [LibraryImport("ole32.dll", EntryPoint = "CoTaskMemFree")]
    public static partial void CoTaskMemFree(nint pv);

    [LibraryImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName")]
    public static partial int SHCreateItemFromParsingName(char* pszPath, nint pbc, Guid* riid, nint* ppv);

    /// <summary>Returns 1 when an icon was chosen; 0 on cancel, and possibly -1 when the dialog failed.</summary>
    [LibraryImport("shell32.dll", EntryPoint = "PickIconDlg")]
    public static partial int PickIconDlg(nint hwnd, char* pszIconPath, uint cchIconPath, int* piIconIndex);

    [LibraryImport("shell32.dll", EntryPoint = "SHGetSetFolderCustomSettings")]
    public static partial int SHGetSetFolderCustomSettings(FolderCustomSettings* pfcs, char* pszPath, uint dwReadWrite);

    [LibraryImport("shell32.dll", EntryPoint = "SHChangeNotify")]
    public static partial void SHChangeNotify(int wEventId, uint uFlags, char* dwItem1, nint dwItem2);

    [LibraryImport("shlwapi.dll", EntryPoint = "PathMakeSystemFolderW")]
    public static partial int PathMakeSystemFolderW(char* pszPath);

    [LibraryImport("shlwapi.dll", EntryPoint = "PathUnmakeSystemFolderW")]
    public static partial int PathUnmakeSystemFolderW(char* pszPath);

    /// <summary>A null value deletes the key; the file keeps its encoding.</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringW")]
    public static partial int WritePrivateProfileStringW(char* lpAppName, char* lpKeyName, char* lpString, char* lpFileName);

    /// <summary>INVALID_HANDLE_VALUE (-1) on failure, not 0.</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true)]
    public static partial nint CreateFileW(char* lpFileName, uint dwDesiredAccess, uint dwShareMode, nint lpSecurityAttributes,
        uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

    [LibraryImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
    public static partial int DeviceIoControl(nint hDevice, uint dwIoControlCode, void* lpInBuffer, uint nInBufferSize,
        void* lpOutBuffer, uint nOutBufferSize, uint* lpBytesReturned, nint lpOverlapped);

    [LibraryImport("kernel32.dll", EntryPoint = "GetDriveTypeW")]
    public static partial uint GetDriveTypeW(char* lpRootPathName);

    /// <summary>SHFOLDERCUSTOMSETTINGS on x64: 104 bytes, pointers 8-aligned.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FolderCustomSettings
    {
        public uint Size;
        public uint Mask;
        public nint ViewId;
        public nint WebViewTemplate;
        public uint WebViewTemplateLength;
        public nint WebViewTemplateVersion;
        public nint InfoTip;
        public uint InfoTipLength;
        public nint Clsid;
        public uint Flags;
        public char* IconFile;
        public uint IconFileLength;
        public int IconIndex;
        public nint Logo;
        public uint LogoLength;
    }
}
