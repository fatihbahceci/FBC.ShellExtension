using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Restarter.Interop;
using static FBC.ShellExtension.Restarter.Interop.Native;

namespace FBC.ShellExtension.Restarter;

/// <param name="Explorer">Restart Manager says the process is Windows Explorer.</param>
internal sealed record ResourceUser(int ProcessId, string Name, bool Explorer);

/// <summary>
/// A Restart Manager session (restartmanager.h). The process that starts it has to stay alive
/// from the shutdown to the restart: Restart Manager does not hand either over (ADR-0008).
/// </summary>
internal sealed unsafe class RestartManagerSession : IDisposable
{
    private readonly uint _handle;

    private RestartManagerSession(uint handle) => _handle = handle;

    public static RestartManagerSession Start()
    {
        uint handle;
        char* key = stackalloc char[CCH_RM_SESSION_KEY + 1];
        new Span<char>(key, CCH_RM_SESSION_KEY + 1).Clear();
        Check(RmStartSession(&handle, 0, key));
        return new RestartManagerSession(handle);
    }

    public void Dispose() => RmEndSession(_handle);

    /// <summary>
    /// Registers processes by id and start time, so a process that has ended and whose id was
    /// reused is not touched.
    /// </summary>
    public void RegisterProcesses(IReadOnlyList<Process> processes)
    {
        var unique = new RM_UNIQUE_PROCESS[processes.Count];
        for (int i = 0; i < processes.Count; i++)
        {
            FILETIME creation, exit, kernel, user;
            if (!GetProcessTimes(processes[i].SafeHandle, &creation, &exit, &kernel, &user))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            unique[i] = new RM_UNIQUE_PROCESS { dwProcessId = processes[i].Id, ProcessStartTime = creation };
        }

        fixed (RM_UNIQUE_PROCESS* pointer = unique)
        {
            Check(RmRegisterResources(_handle, 0, null, (uint)unique.Length, pointer, 0, null));
        }
    }

    public void RegisterFiles(IReadOnlyList<string> files)
    {
        var pointers = new nint[files.Count];
        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                pointers[i] = Marshal.StringToHGlobalUni(Path.GetFullPath(files[i]));
            }

            fixed (nint* names = pointers)
            {
                Check(RmRegisterResources(_handle, (uint)files.Count, (char**)names, 0, null, 0, null));
            }
        }
        finally
        {
            foreach (nint pointer in pointers)
            {
                Marshal.FreeHGlobal(pointer);
            }
        }
    }

    /// <summary>The processes that use the registered resources; the list may grow between calls.</summary>
    public List<ResourceUser> Users()
    {
        uint count = 0;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            uint needed, reasons;
            var infos = new RM_PROCESS_INFO[Math.Max(count, 1)];
            fixed (RM_PROCESS_INFO* pointer = infos)
            {
                uint capacity = (uint)infos.Length;
                int result = RmGetList(_handle, &needed, &capacity, pointer, &reasons);
                if (result == ERROR_MORE_DATA)
                {
                    count = needed + 4;
                    continue;
                }

                Check(result);
                var users = new List<ResourceUser>((int)capacity);
                for (int i = 0; i < capacity; i++)
                {
                    users.Add(new ResourceUser(
                        pointer[i].Process.dwProcessId,
                        new string(pointer[i].strAppName),
                        pointer[i].ApplicationType == RmExplorer));
                }

                return users;
            }
        }

        throw new Win32Exception(ERROR_MORE_DATA);
    }

    /// <returns>The Win32 error code; 0 when everything registered was shut down.</returns>
    public int Shutdown(bool force) => RmShutdown(_handle, force ? RmForceShutdown : 0, 0);

    public int Restart() => RmRestart(_handle, 0, 0);

    private static void Check(int error)
    {
        if (error != 0)
        {
            throw new Win32Exception(error);
        }
    }
}
