using System.ComponentModel;
using System.Runtime.InteropServices;
using FBC.ShellExtension.ShellExt.Interop;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>Starts one of our programs from inside the host process and lets go of it at once.</summary>
internal static unsafe class ProcessStarter
{
    public static void Start(string application, string commandLine)
    {
        var startup = new STARTUPINFOW { cb = (uint)sizeof(STARTUPINFOW) };
        PROCESS_INFORMATION process;

        // CreateProcessW may write into the command line, so it gets its own buffer.
        char[] buffer = new char[commandLine.Length + 1];
        commandLine.CopyTo(buffer);

        // Handles are not inherited: the program gets nothing of Explorer's.
        fixed (char* app = application)
        fixed (char* cmd = buffer)
        {
            if (!CreateProcess(app, cmd, 0, 0, false, 0, 0, null, &startup, &process))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }

        CloseHandle(process.hThread);
        CloseHandle(process.hProcess);
    }
}
