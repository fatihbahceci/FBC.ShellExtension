using FBC.ShellExtension.Restarter.Interop;

namespace FBC.ShellExtension.Restarter;

/// <summary>
/// Runs shell work on its own STA thread with a time limit. A call into a hung Explorer window
/// never returns; the restart is often meant to rescue exactly that, so the caller gives up on
/// the thread instead of waiting for it.
/// </summary>
internal static class StaThread
{
    /// <returns>False when the time ran out; the thread is left behind as a background thread.</returns>
    public static bool Run(Action work, TimeSpan timeout, out Exception? error)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            int hr = Native.CoInitializeEx(0, Native.COINIT_APARTMENTTHREADED);
            try
            {
                work();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (hr >= 0)
                {
                    Native.CoUninitialize();
                }
            }
        })
        {
            IsBackground = true,
            Name = "Shell",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(timeout);
        error = failure;
        return finished;
    }
}
