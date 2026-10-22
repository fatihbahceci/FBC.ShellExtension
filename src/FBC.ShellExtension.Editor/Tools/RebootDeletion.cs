using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Has Windows delete items at the next restart (MoveFileEx, MOVEFILE_DELAY_UNTIL_REBOOT), which
/// only an administrator may ask for (ADR-0031). The window starts the editor again, elevated,
/// with the paths on its command line: not in a file, which another program of the user could
/// change between the UAC question and the reading.
/// </summary>
internal static partial class RebootDeletion
{
    public const string Argument = "--delete-at-reboot";

    private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;
    private const int ERROR_CANCELLED = 1223;

    // ShellExecute, which runas goes through, is documented to take about 2,048 characters
    // (INTERNET_MAX_URL_LENGTH); beyond that it may cut the list, so it is refused here.
    private const int MaxArguments = 2_000;

    /// <summary>From the window: the elevated editor marks the items; UAC asks first.</summary>
    /// <returns>True when all were marked; false when the elevated editor said what failed; null when UAC was declined.</returns>
    public static async Task<bool?> RequestAsync(IWin32Window owner, string title, IReadOnlyList<string> paths)
    {
        string arguments = string.Join(' ', paths.Select(Quote).Prepend(Argument));
        if (arguments.Length > MaxArguments)
        {
            MessageBox.Show(owner, Strings.LockedArgumentsTooLong(), title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? Application.ExecutablePath, arguments)
            {
                UseShellExecute = true,
                Verb = "runas",
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            return null;
        }
        catch (Win32Exception ex)
        {
            MessageBox.Show(owner, Strings.LockedElevationFailed(ex.Message), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (process is null)
        {
            return false;
        }

        using (process)
        {
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
    }

    /// <summary>
    /// In the elevated editor: marks every item, what is in a folder before the folder. A link
    /// goes at once instead: whether Windows' boot-time deletion removes a link or what it leads
    /// to is not documented, and removing a link now touches nothing it leads to.
    /// </summary>
    /// <returns>0 when all were marked, 1 when something failed and was shown.</returns>
    public static int Mark(IReadOnlyList<string> paths)
    {
        IReadOnlyList<RebootEntry> order = FileTools.RebootDeletionOrder(paths, out List<string> errors);
        foreach ((string path, bool link, bool folder) in order)
        {
            if (link)
            {
                try
                {
                    // The link itself goes (RemoveDirectory on a junction), never what it leads to.
                    if (folder)
                    {
                        Directory.Delete(path, recursive: false);
                    }
                    else
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(path, ex.Message));
                }
            }
            else if (!MoveFileExW(path, null, MOVEFILE_DELAY_UNTIL_REBOOT))
            {
                errors.Add(Strings.ToolItemFailed(path, new Win32Exception(Marshal.GetLastPInvokeError()).Message));
            }
        }

        ToolWindows.ShowErrors(null, ProductInfo.Name, Strings.LockedNotScheduled(), errors);
        return errors.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// One command line argument: quoted, and a closing backslash doubled so the quote stays one
    /// (C:\ would otherwise escape it). A path holds no quote of its own.
    /// </summary>
    private static string Quote(string path) => path.EndsWith('\\') ? $"\"{path}\\\"" : $"\"{path}\"";

    [LibraryImport("kernel32.dll", EntryPoint = "MoveFileExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MoveFileExW(string existing, string? replacement, uint flags);
}
