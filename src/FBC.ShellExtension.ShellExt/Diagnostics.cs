using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>
/// Trace goes to OutputDebugString: invisible unless a debugger or Sysinternals DebugView is
/// listening, and free to call from inside Explorer. Lines start with "[FBCTools]".
/// </summary>
internal static class Diagnostics
{
    public static void Trace(string message) => OutputDebugString($"[FBCTools] {message}\n");

    /// <summary>Records the exception and turns it into the HRESULT to return.</summary>
    public static int Fail(string where, Exception ex, int fallback = E_FAIL)
    {
        Trace($"{where} failed: {ex}");
        return ex.HResult < 0 ? ex.HResult : fallback;
    }
}
