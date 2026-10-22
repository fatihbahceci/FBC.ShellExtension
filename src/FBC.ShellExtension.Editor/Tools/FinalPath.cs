using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Where a folder really is, as Windows resolves it: through a mapped drive, a subst drive or a
/// junction (GetFinalPathNameByHandle). Two different spellings of one place give the same answer.
/// </summary>
internal static unsafe partial class FinalPath
{
    private const uint FILE_SHARE_ALL = 0x7;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint VOLUME_NAME_DOS = 0;
    private const int MaxLength = 32_768;

    /// <summary>The folder's final path (<c>\\?\C:\…</c> or <c>\\?\UNC\server\share\…</c>); null when Windows cannot say.</summary>
    public static string? Of(string folder)
    {
        // No access asked for: the name is all that is read, a folder only being looked at is enough.
        using SafeFileHandle handle = CreateFileW(folder, 0, FILE_SHARE_ALL, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
        if (handle.IsInvalid)
        {
            return null;
        }

        char* buffer = stackalloc char[MaxLength];
        uint length = GetFinalPathNameByHandleW(handle, buffer, MaxLength, VOLUME_NAME_DOS);
        return length is > 0 and < MaxLength ? new string(buffer, 0, (int)length) : null;
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint GetFinalPathNameByHandleW(SafeFileHandle file, char* path, uint length, uint flags);
}
