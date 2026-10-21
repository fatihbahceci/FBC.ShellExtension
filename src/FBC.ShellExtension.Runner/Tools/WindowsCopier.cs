using System.ComponentModel;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Duplicate's copy through CopyFileEx: what Explorer's copy keeps comes along, the
/// "downloaded from the internet" mark among it, and a cancel removes the half-written file
/// (ADR-0018). Progress comes back through a callback on the copying thread.
/// </summary>
internal sealed unsafe class WindowsCopier : IFileCopier
{
    public void Copy(string source, string target, Action<long, long> progress, CancellationToken cancellationToken)
    {
        var state = new State(progress, cancellationToken);
        GCHandle handle = GCHandle.Alloc(state);
        try
        {
            if (CopyFileExW(source, target, &Progress, GCHandle.ToIntPtr(handle), null, COPY_FILE_FAIL_IF_EXISTS))
            {
                return;
            }

            int error = Marshal.GetLastPInvokeError();
            if (state.Failure is { } failure)
            {
                throw failure;
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new IOException(new Win32Exception(error).Message);
        }
        finally
        {
            handle.Free();
        }
    }

    [UnmanagedCallersOnly]
    private static uint Progress(long totalFileSize, long totalBytesTransferred, long streamSize, long streamBytesTransferred,
        uint streamNumber, uint callbackReason, nint sourceFile, nint destinationFile, nint data)
    {
        var state = (State)GCHandle.FromIntPtr(data).Target!;
        try
        {
            if (state.CancellationToken.IsCancellationRequested)
            {
                return PROGRESS_CANCEL;
            }

            state.Progress(totalBytesTransferred, totalFileSize);
            return PROGRESS_CONTINUE;
        }
        catch (Exception ex)
        {
            // Nothing may cross back into CopyFileEx; the copy is cancelled and the error kept.
            state.Failure = ex;
            return PROGRESS_CANCEL;
        }
    }

    private sealed class State(Action<long, long> progress, CancellationToken cancellationToken)
    {
        public Action<long, long> Progress { get; } = progress;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public Exception? Failure { get; set; }
    }
}
