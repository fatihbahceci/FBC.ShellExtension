using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FBC.ShellExtension.Editor;

internal static partial class SingleInstance
{
    private const int SW_RESTORE = 9;

    public static void ActivateExisting()
    {
        using Process current = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(current.ProcessName))
        {
            using (process)
            {
                if (process.Id == current.Id || process.MainWindowHandle == 0)
                {
                    continue;
                }

                if (IsIconic(process.MainWindowHandle))
                {
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                }

                SetForegroundWindow(process.MainWindowHandle);
                return;
            }
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll", EntryPoint = "IsIconic")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint hWnd);
}
