using System.Runtime.InteropServices;

namespace FBC.ShellExtension.Manager;

internal static unsafe partial class Native
{
    private const nint HWND_BROADCAST = 0xFFFF;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint SEM_FAILCRITICALERRORS = 0x0001;

    /// <summary>
    /// "Environment" changed: Explorer reloads its environment, so programs it starts later see
    /// the new PATH. Synchronous with a time limit; an asynchronous broadcast cannot carry the
    /// string (ADR-0011).
    /// </summary>
    public static void BroadcastEnvironmentChange()
    {
        nint result;
        fixed (char* area = "Environment")
        {
            SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, (nint)area, SMTO_ABORTIFHUNG, 5000, &result);
        }
    }

    /// <summary>A missing DLL in a probed ffmpeg must end the process, not open a dialog nobody sees.</summary>
    public static void FailCriticalErrorsQuietly() => SetErrorMode(SEM_FAILCRITICALERRORS);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    private static partial nint SendMessageTimeout(nint hwnd, uint message, nint wParam, nint lParam, uint flags, uint timeout, nint* result);

    [LibraryImport("kernel32.dll", EntryPoint = "SetErrorMode")]
    private static partial uint SetErrorMode(uint mode);
}
