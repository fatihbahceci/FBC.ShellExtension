using System.ComponentModel;
using System.Runtime.InteropServices;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>
/// Where this DLL lives. AppContext.BaseDirectory would name the host process's folder
/// (Explorer's), so the path is asked of the loader, by an address inside this module.
/// </summary>
internal static unsafe class Module
{
    private static string? s_path;

    public static string Path => s_path ??= Resolve();

    public static string Directory => System.IO.Path.GetDirectoryName(Path)!;

    private static string Resolve()
    {
        nint address = (nint)(delegate* unmanaged<int>)&Exports.DllCanUnloadNow;
        if (!GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, address, out nint module))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        char[] buffer = new char[32768];
        fixed (char* p = buffer)
        {
            uint length = GetModuleFileName(module, p, (uint)buffer.Length);
            if (length == 0 || length >= buffer.Length)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            return new string(p, 0, (int)length);
        }
    }
}
