using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>What Restart Manager says a program is (RM_APP_TYPE).</summary>
internal enum HolderKind
{
    Unknown = 0,
    MainWindow = 1,
    OtherWindow = 2,
    Service = 3,
    Explorer = 4,
    Console = 5,
    Critical = 1000,
}

/// <summary>A program that has one of the files open; its start time tells it from a later one with the same id.</summary>
internal sealed record Holder(int ProcessId, long StartTime, string Name, HolderKind Kind)
{
    /// <summary>
    /// Asked to close like a user would close it. Explorer, services and Windows' own processes
    /// are not: closing them takes the desktop, a service or Windows with them.
    /// </summary>
    public bool Closable => Kind is HolderKind.Unknown or HolderKind.MainWindow or HolderKind.OtherWindow or HolderKind.Console;
}

/// <summary>
/// A Restart Manager session that only asks who uses the files (restartmanager.h), as the
/// restarter does for Explorer (ADR-0008). RmShutdown is not used: it closes every user of the
/// files at once, Explorer too (ADR-0031).
/// </summary>
internal sealed unsafe partial class RestartManager : IDisposable
{
    private const int ERROR_MORE_DATA = 234;
    private const int CCH_RM_SESSION_KEY = 32;

    private readonly uint _session;

    private RestartManager(uint session) => _session = session;

    /// <exception cref="Win32Exception">Restart Manager refused.</exception>
    public static RestartManager ForFiles(IReadOnlyList<string> files)
    {
        uint session;
        char* key = stackalloc char[CCH_RM_SESSION_KEY + 1];
        Check(RmStartSession(&session, 0, key));
        var manager = new RestartManager(session);
        var names = new nint[files.Count];
        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                names[i] = Marshal.StringToHGlobalUni(files[i]);
            }

            fixed (nint* pointers = names)
            {
                Check(RmRegisterResources(session, (uint)files.Count, (char**)pointers, 0, null, 0, null));
            }

            return manager;
        }
        catch
        {
            manager.Dispose();
            throw;
        }
        finally
        {
            foreach (nint name in names)
            {
                Marshal.FreeHGlobal(name);
            }
        }
    }

    public void Dispose() => RmEndSession(_session);

    /// <summary>The programs that use the files now; asked again, the answer follows what closed meanwhile.</summary>
    /// <exception cref="Win32Exception">Restart Manager refused.</exception>
    public List<Holder> Holders()
    {
        uint size = 4;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            var infos = new RM_PROCESS_INFO[size];
            uint needed;
            uint count = size;
            uint reasons;
            fixed (RM_PROCESS_INFO* pointer = infos)
            {
                int result = RmGetList(_session, &needed, &count, pointer, &reasons);
                if (result == ERROR_MORE_DATA)
                {
                    // The list grows while programs open the files; asked again with room to spare.
                    size = needed + 4;
                    continue;
                }

                Check(result);
                var holders = new List<Holder>((int)count);
                for (int i = 0; i < count; i++)
                {
                    RM_PROCESS_INFO info = pointer[i];
                    long startTime = ((long)info.StartTimeHigh << 32) | info.StartTimeLow;
                    holders.Add(new Holder(info.ProcessId, startTime, new string(info.AppName), (HolderKind)info.ApplicationType));
                }

                return holders;
            }
        }

        throw new Win32Exception(ERROR_MORE_DATA);
    }

    private static void Check(int error)
    {
        if (error != 0)
        {
            throw new Win32Exception(error);
        }
    }

    /// <summary>
    /// RM_PROCESS_INFO: RM_UNIQUE_PROCESS (the id and a FILETIME, two DWORDs: no padding before
    /// it, unlike a long), two names, the type, status, session, restartable.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RM_PROCESS_INFO
    {
        public int ProcessId;
        public uint StartTimeLow;
        public uint StartTimeHigh;
        public fixed char AppName[256];
        public fixed char ServiceShortName[64];
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        public int Restartable;
    }

    [LibraryImport("rstrtmgr.dll")]
    private static partial int RmStartSession(uint* session, uint flags, char* sessionKey);

    [LibraryImport("rstrtmgr.dll")]
    private static partial int RmRegisterResources(uint session, uint fileCount, char** files, uint processCount, void* processes, uint serviceCount, char** services);

    [LibraryImport("rstrtmgr.dll")]
    private static partial int RmGetList(uint session, uint* needed, uint* count, RM_PROCESS_INFO* processes, uint* rebootReasons);

    [LibraryImport("rstrtmgr.dll")]
    private static partial int RmEndSession(uint session);
}
