using System.ComponentModel;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Restart;
using FBC.ShellExtension.Restarter.Interop;
using static FBC.ShellExtension.Restarter.Interop.Native;

namespace FBC.ShellExtension.Restarter;

/// <summary>
/// Opens saved windows again, each with its tabs, active tab and placement. Explorer has no
/// documented way to open a tab, so tabs use the commands open-source tab tools send to
/// Explorer's windows (ADR-0008); every step is checked, and a tab that cannot be made opens as
/// a window of its own. Runs on the STA thread that owns <paramref name="windows"/>.
/// </summary>
internal sealed unsafe class WindowRestorer(FolderWindows windows, Action<string> warn)
{
    // Undocumented WM_COMMAND ids of a Windows 11 folder window: "new tab" goes to the tab in
    // front, "select tab" to the frame with the 1-based tab position (ExplorerTabUtility, MIT).
    private const int NewTabCommand = 0xA21B;
    private const int SelectTabCommand = 0xA221;

    private static readonly TimeSpan s_windowTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_tabTimeout = TimeSpan.FromSeconds(4);

    private static readonly TimeSpan s_navigateTimeout = TimeSpan.FromSeconds(10);

    private enum TabResult
    {
        NotCreated,
        CreatedNotNavigated,
        Navigated,
    }

    public void Restore(IReadOnlyList<SavedWindow> missing)
    {
        foreach (SavedWindow window in missing)
        {
            nint frame = OpenWindow(window.Tabs[0]);
            if (frame == 0)
            {
                continue;
            }

            // Position in the window of each saved tab that became a tab there; -1 otherwise.
            var positions = new int[window.Tabs.Count];
            int created = 1;
            for (int i = 1; i < window.Tabs.Count; i++)
            {
                switch (OpenTab(frame, window.Tabs[i]))
                {
                    case TabResult.NotCreated:
                        positions[i] = -1;
                        warn(Strings.RestarterTabAsWindow(Name(window.Tabs[i])));
                        OpenWindow(window.Tabs[i]);
                        break;
                    case TabResult.CreatedNotNavigated:
                        // The tab exists, so it keeps its place; a window of its own would add a second copy.
                        positions[i] = created++;
                        warn(Strings.RestarterTabNotNavigated(Name(window.Tabs[i])));
                        break;
                    default:
                        positions[i] = created++;
                        break;
                }
            }

            int front = positions[Math.Clamp(window.ActiveTab, 0, positions.Length - 1)];
            if (created > 1 && front >= 0)
            {
                nint ignored;
                SendMessageTimeout(frame, WM_COMMAND, SelectTabCommand, front + 1, SMTO_ABORTIFHUNG, 2000, &ignored);
            }

            // Last, because opening tabs can move the window.
            FolderWindows.ApplyPlacement(frame, window);
        }
    }

    private static string Name(SavedTab tab) => tab.DisplayName.Length > 0 ? tab.DisplayName : "?";

    /// <returns>The new folder window, or 0 when none appeared.</returns>
    private nint OpenWindow(SavedTab tab)
    {
        HashSet<nint> before = [.. FolderWindows.FramesInZOrder()];

        // "opennewwindow" keeps a new window a window even where Explorer opens folders as tabs.
        if (!Execute(tab, "opennewwindow", out int error) && !Execute(tab, null, out error))
        {
            warn(Strings.RestarterWindowFailed(Name(tab), new Win32Exception(error).Message));
            return 0;
        }

        nint frame = WaitFor(() => FolderWindows.FramesInZOrder().FirstOrDefault(f => !before.Contains(f)), s_windowTimeout);
        if (frame == 0)
        {
            warn(Strings.RestarterWindowFailed(Name(tab), Strings.RestarterNoWindow()));
            return 0;
        }

        // Its tab registers with IShellWindows once the first folder is shown.
        WaitFor(() => FindTab(frame, _ => true) ? 1 : 0, s_windowTimeout);
        return frame;
    }

    private static bool Execute(SavedTab tab, string? verb, out int error)
    {
        nint idList = ShellCom.IdListFromBytes(tab.IdList);
        try
        {
            fixed (char* verbText = verb)
            {
                var info = new SHELLEXECUTEINFOW
                {
                    cbSize = (uint)sizeof(SHELLEXECUTEINFOW),
                    fMask = SEE_MASK_IDLIST | SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI,
                    lpVerb = (nint)verbText,
                    lpIDList = idList,
                    nShow = SW_SHOWNORMAL,
                };

                bool started = ShellExecuteEx(&info);
                error = started ? 0 : Marshal.GetLastPInvokeError();
                return started;
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(idList);
        }
    }

    private TabResult OpenTab(nint frame, SavedTab tab)
    {
        List<nint> before = FolderWindows.TabWindows(frame);
        if (before.Count == 0 || !PostMessage(before[0], WM_COMMAND, NewTabCommand, 0))
        {
            return TabResult.NotCreated;
        }

        nint added = WaitFor(() => FolderWindows.TabWindows(frame).FirstOrDefault(t => !before.Contains(t)), s_tabTimeout);
        if (added == 0)
        {
            return TabResult.NotCreated;
        }

        // The tab's browser shows up in IShellWindows a moment after its window does, later
        // still while a freshly started Explorer is busy.
        bool navigated = false;
        WaitFor(() => FindTab(frame, live => live.TabWindow == added && (navigated = Navigate(live, tab))) ? 1 : 0, s_navigateTimeout);
        return navigated ? TabResult.Navigated : TabResult.CreatedNotNavigated;
    }

    private static bool Navigate(LiveTab live, SavedTab tab)
    {
        nint idList = ShellCom.IdListFromBytes(tab.IdList);
        try
        {
            if (ShellCom.BrowseObject(live.Browser, idList, SBSP_SAMEBROWSER | SBSP_ABSOLUTE) == 0)
            {
                return true;
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(idList);
        }

        return tab.DisplayName.Length > 0 && ShellCom.Navigate(live.Dispatch, tab.DisplayName) == 0;
    }

    /// <summary>Whether one of the frame's tabs satisfies <paramref name="match"/>, which runs while the tab is still held.</summary>
    private bool FindTab(nint frame, Func<LiveTab, bool> match)
    {
        List<LiveTab> tabs = windows.Tabs();
        try
        {
            return tabs.Any(tab => tab.Frame == frame && match(tab));
        }
        finally
        {
            tabs.ForEach(tab => tab.Dispose());
        }
    }

    private static nint WaitFor(Func<nint> probe, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            nint result = probe();
            if (result != 0 || DateTime.UtcNow >= deadline)
            {
                return result;
            }

            Thread.Sleep(100);
        }
    }
}
