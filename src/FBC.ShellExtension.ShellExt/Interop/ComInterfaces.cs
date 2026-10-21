using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FBC.ShellExtension.ShellExt.Interop;

// Signatures follow unknwn.h and shobjidl_core.h. Every method returns its HRESULT itself
// ([PreserveSig]) and uses blittable types only, so the generated stubs do no marshalling.

[GeneratedComInterface]
[Guid("00000001-0000-0000-C000-000000000046")]
internal unsafe partial interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(nint pUnkOuter, Guid* riid, nint* ppvObject);

    [PreserveSig]
    int LockServer(int fLock);
}

[GeneratedComInterface]
[Guid("000214E8-0000-0000-C000-000000000046")]
internal partial interface IShellExtInit
{
    [PreserveSig]
    int Initialize(nint pidlFolder, nint pdtobj, nint hkeyProgId);
}

[GeneratedComInterface]
[Guid("000214E4-0000-0000-C000-000000000046")]
internal unsafe partial interface IContextMenu
{
    [PreserveSig]
    int QueryContextMenu(nint hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);

    [PreserveSig]
    int InvokeCommand(CMINVOKECOMMANDINFO* pici);

    [PreserveSig]
    int GetCommandString(nuint idCmd, uint uType, uint* pReserved, byte* pszName, uint cchMax);
}

[StructLayout(LayoutKind.Sequential)]
internal struct CMINVOKECOMMANDINFO
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

[StructLayout(LayoutKind.Sequential)]
internal struct FORMATETC
{
    public ushort cfFormat;
    public nint ptd;
    public uint dwAspect;
    public int lindex;
    public uint tymed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct STGMEDIUM
{
    public uint tymed;
    public nint unionmember;
    public nint pUnkForRelease;
}

[StructLayout(LayoutKind.Sequential)]
internal struct STARTUPINFOW
{
    public uint cb;
    public nint lpReserved;
    public nint lpDesktop;
    public nint lpTitle;
    public uint dwX;
    public uint dwY;
    public uint dwXSize;
    public uint dwYSize;
    public uint dwXCountChars;
    public uint dwYCountChars;
    public uint dwFillAttribute;
    public uint dwFlags;
    public ushort wShowWindow;
    public ushort cbReserved2;
    public nint lpReserved2;
    public nint hStdInput;
    public nint hStdOutput;
    public nint hStdError;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROCESS_INFORMATION
{
    public nint hProcess;
    public nint hThread;
    public uint dwProcessId;
    public uint dwThreadId;
}
