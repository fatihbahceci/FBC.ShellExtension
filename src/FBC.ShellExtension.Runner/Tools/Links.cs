using System.ComponentModel;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// The links Create link makes (ADR-0026): a junction for a folder, which needs no
/// administrator rights but only reaches folders on this computer's drives; a symbolic link for
/// a file, which needs Developer Mode or administrator rights.
/// </summary>
internal static unsafe class Links
{
    /// <summary>Makes <paramref name="link"/>, a new folder, a junction to the folder <paramref name="target"/>.</summary>
    /// <exception cref="ToolFailedException">The target or the link is not on a local drive, or Windows refused; nothing is left behind.</exception>
    public static void CreateJunction(string link, string target)
    {
        string name = Path.GetFileName(link);
        byte[] data = FileTools.MountPointData(target);
        if (IsRemote(target))
        {
            throw new ToolFailedException(Strings.ToolLinkNotLocal(name));
        }

        // A server would take it, and resolve \??\C:\… on its own drives.
        if (IsRemote(link))
        {
            throw new ToolFailedException(Strings.ToolLinkPlaceNotLocal(name));
        }

        Directory.CreateDirectory(link);
        int error = 0;
        nint handle;
        fixed (char* path = link)
        {
            handle = CreateFileW(path, GENERIC_WRITE, 0, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT, 0);
        }

        if (handle == -1)
        {
            error = Marshal.GetLastPInvokeError();
        }
        else
        {
            uint returned = 0;
            fixed (byte* input = data)
            {
                if (DeviceIoControl(handle, FSCTL_SET_REPARSE_POINT, input, (uint)data.Length, null, 0, &returned, 0) == 0)
                {
                    error = Marshal.GetLastPInvokeError();
                }
            }

            // Closed before the folder can go: it is only deleted once no handle is open.
            CloseHandle(handle);
        }

        if (error != 0)
        {
            try
            {
                Directory.Delete(link);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The failure to report is the one above.
            }

            throw new ToolFailedException(Strings.ToolLinkFailed(name, new Win32Exception(error).Message));
        }
    }

    /// <summary>Makes <paramref name="link"/> a symbolic link to the file <paramref name="target"/>.</summary>
    /// <exception cref="ToolFailedException">Windows refused; when for want of the right, the message says what grants it.</exception>
    public static void CreateFileLink(string link, string target)
    {
        string name = Path.GetFileName(link);
        try
        {
            // .NET asks for the unprivileged form, which Developer Mode allows.
            File.CreateSymbolicLink(link, target);
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_PRIVILEGE_NOT_HELD)
        {
            throw new ToolFailedException(Strings.ToolLinkNeedsPrivilege(name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ToolFailedException(Strings.ToolLinkFailed(name, ex.Message));
        }
    }

    /// <summary>On a network share or a mapped network drive.</summary>
    private static bool IsRemote(string path)
    {
        string? root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        // \\server\share and \\?\UNC\server\share; \\?\C:\ is a local drive written long.
        if (root.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return !(root.Length > 5 && root[5] == ':');
        }

        fixed (char* text = root)
        {
            return GetDriveTypeW(text) == DRIVE_REMOTE;
        }
    }
}
