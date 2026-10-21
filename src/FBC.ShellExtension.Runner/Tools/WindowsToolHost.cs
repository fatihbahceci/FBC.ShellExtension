using System.Text;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.Execution;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>The real file system and network names behind the clipboard tools.</summary>
internal sealed unsafe class WindowsToolHost : IToolHost
{
    private const uint InitialBufferSize = 1024;

    public Encoding Ansi => CodePages.Ansi;

    public string? Title(string path) => ShellProperties.Title(path);

    /// <summary>
    /// Shares the file the way Notepad does: a log another program is still writing can be read.
    /// <see cref="File.ReadAllBytes"/> shares reading only and fails on it.
    /// </summary>
    public byte[] ReadFile(string path, long maxBytes)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        long limit = maxBytes + 1;
        int read;
        while (buffer.Length < limit && (read = stream.Read(chunk, 0, (int)Math.Min(chunk.Length, limit - buffer.Length))) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    public string? UniversalName(string path)
    {
        // A local path, or one that is already UNC, fails with ERROR_NOT_CONNECTED and stays as it is.
        uint size = InitialBufferSize;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            byte[] buffer = new byte[size];
            fixed (byte* start = buffer)
            {
                int result = WNetGetUniversalNameW(path, UNIVERSAL_NAME_INFO_LEVEL, start, ref size);
                if (result == 0)
                {
                    // UNIVERSAL_NAME_INFOW: one pointer to the name, which follows in the same buffer.
                    char* name = *(char**)start;
                    return name == null ? null : new string(name);
                }

                if (result != ERROR_MORE_DATA)
                {
                    return null;
                }
            }
        }

        return null;
    }
}
