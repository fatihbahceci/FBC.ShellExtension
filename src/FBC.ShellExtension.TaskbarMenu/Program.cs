using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.TaskbarMenu;

/// <summary>
/// Pinned to the taskbar by the user (ADR-0043): starts the runner next to it with the taskbar
/// argument and ends. The runner opens the menu; it may take the foreground, which this program
/// has, having just been clicked.
/// </summary>
internal static partial class Program
{
    private const uint ASFW_ANY = unchecked((uint)-1);
    private const uint MB_ICONERROR = 0x10;

    private static int Main()
    {
        string runner = Path.Combine(AppContext.BaseDirectory, ProductInfo.RunnerFileName);
        try
        {
            AllowSetForegroundWindow(ASFW_ANY);
            using Process? process = Process.Start(new ProcessStartInfo(runner, ProductInfo.TaskbarMenuArgument)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.SystemDirectory,
            });
            return 0;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException)
        {
            MessageBoxW(0, Strings.TaskbarMenuRunnerFailed(runner, ex.Message), ProductInfo.Name, MB_ICONERROR);
            return 1;
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "AllowSetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllowSetForegroundWindow(uint processId);

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBoxW(nint hWnd, string lpText, string lpCaption, uint uType);
}
