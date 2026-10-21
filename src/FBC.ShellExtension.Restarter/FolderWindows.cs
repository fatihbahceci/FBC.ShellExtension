using FBC.ShellExtension.Core.Restart;
using FBC.ShellExtension.Restarter.Interop;
using static FBC.ShellExtension.Restarter.Interop.Native;

namespace FBC.ShellExtension.Restarter;

/// <summary>
/// One open tab as Explorer reports it now. Holds references to Explorer's objects for the tab;
/// release it on the thread that made it.
/// </summary>
internal sealed class LiveTab(nint frame, nint tabWindow, byte[] idList, string displayName, nint dispatch, nint browser) : IDisposable
{
    public nint Frame { get; } = frame;

    public nint TabWindow { get; } = tabWindow;

    public byte[] IdList { get; } = idList;

    public string DisplayName { get; } = displayName;

    public nint Dispatch { get; } = dispatch;

    public nint Browser { get; } = browser;

    public void Dispose()
    {
        ShellCom.Release(Browser);
        ShellCom.Release(Dispatch);
    }
}

/// <summary>Reads Explorer's folder windows and tabs. Every method runs on the STA thread that created it.</summary>
internal sealed unsafe class FolderWindows : IDisposable
{
    private const string TabClass = "ShellTabWindowClass";
    private readonly nint _shellWindows = ShellCom.CreateShellWindows();

    public void Dispose() => ShellCom.Release(_shellWindows);

    public static bool IsFolderFrame(nint hwnd)
    {
        char* buffer = stackalloc char[64];
        int length = GetClassName(hwnd, buffer, 64);
        var name = new ReadOnlySpan<char>(buffer, length);

        // A window opened with /e is an ExplorerWClass; it is the same kind of window.
        return name.SequenceEqual("CabinetWClass") || name.SequenceEqual("ExplorerWClass");
    }

    /// <summary>Folder windows, topmost first.</summary>
    public static List<nint> FramesInZOrder()
    {
        var frames = new List<nint>();
        for (nint hwnd = GetTopWindow(0); hwnd != 0; hwnd = GetWindow(hwnd, GW_HWNDNEXT))
        {
            if (IsFolderFrame(hwnd))
            {
                frames.Add(hwnd);
            }
        }

        return frames;
    }

    /// <summary>The tab windows of a frame, the one in front first.</summary>
    public static List<nint> TabWindows(nint frame)
    {
        var tabs = new List<nint>();
        for (nint tab = FindWindowEx(frame, 0, TabClass, null); tab != 0; tab = FindWindowEx(frame, tab, TabClass, null))
        {
            tabs.Add(tab);
        }

        return tabs;
    }

    /// <summary>Every tab of every folder window, in the order IShellWindows lists them.</summary>
    public List<LiveTab> Tabs()
    {
        var tabs = new List<LiveTab>();
        int count = ShellCom.GetCount(_shellWindows);
        for (int i = 0; i < count; i++)
        {
            if (Read(ShellCom.Item(_shellWindows, i)) is { } tab)
            {
                tabs.Add(tab);
            }
        }

        return tabs;
    }

    private static LiveTab? Read(nint dispatch)
    {
        if (dispatch == 0)
        {
            return null;
        }

        nint services = 0, browser = 0, view = 0, folderView = 0, folder = 0, idList = 0;
        bool kept = false;
        try
        {
            services = ShellCom.QueryInterface(dispatch, ShellCom.IID_IServiceProvider);
            if (services == 0)
            {
                return null;
            }

            // The tab's own browser; SID_STopLevelBrowser is the fallback for an Explorer that
            // does not answer the first (ADR-0008).
            browser = ShellCom.QueryService(services, ShellCom.IID_IShellBrowser, ShellCom.IID_IShellBrowser);
            if (browser == 0)
            {
                browser = ShellCom.QueryService(services, ShellCom.SID_STopLevelBrowser, ShellCom.IID_IShellBrowser);
            }

            nint tabWindow = browser == 0 ? 0 : ShellCom.GetWindow(browser);
            nint frame = tabWindow == 0 ? 0 : GetAncestor(tabWindow, GA_ROOT);
            if (frame == 0 || !IsFolderFrame(frame))
            {
                return null;
            }

            view = ShellCom.QueryActiveShellView(browser);
            folderView = view == 0 ? 0 : ShellCom.QueryInterface(view, ShellCom.IID_IFolderView);
            folder = folderView == 0 ? 0 : ShellCom.GetFolder(folderView, ShellCom.IID_IPersistFolder2);
            idList = folder == 0 ? 0 : ShellCom.GetCurFolder(folder);
            if (idList == 0)
            {
                return null;
            }

            kept = true;
            return new LiveTab(frame, tabWindow, ShellCom.IdListBytes(idList), ShellCom.ParsingName(idList), dispatch, browser);
        }
        finally
        {
            if (idList != 0)
            {
                System.Runtime.InteropServices.Marshal.FreeCoTaskMem(idList);
            }

            ShellCom.Release(folder);
            ShellCom.Release(folderView);
            ShellCom.Release(view);
            ShellCom.Release(services);
            if (!kept)
            {
                ShellCom.Release(browser);
                ShellCom.Release(dispatch);
            }
        }
    }

    /// <summary>The windows as they are now: bottom of the z-order first, tabs and placement each.</summary>
    public ExplorerSnapshot Snapshot()
    {
        List<LiveTab> tabs = Tabs();
        try
        {
            var windows = new List<SavedWindow>();
            List<nint> frames = FramesInZOrder();
            frames.AddRange(tabs.Select(tab => tab.Frame).Distinct().Where(frame => !frames.Contains(frame)));
            frames.Reverse();
            foreach (nint frame in frames)
            {
                List<LiveTab> own = [.. tabs.Where(tab => tab.Frame == frame)];
                if (own.Count == 0)
                {
                    continue;
                }

                // A window closed while the others were being read has no placement; it is not
                // brought back.
                if (ReadPlacement(frame) is not { } placement)
                {
                    continue;
                }

                nint front = FindWindowEx(frame, 0, TabClass, null);
                int active = Math.Max(0, own.FindIndex(tab => tab.TabWindow == front));
                windows.Add(new SavedWindow(
                    [.. own.Select(tab => new SavedTab(tab.IdList, tab.DisplayName))],
                    active,
                    placement,
                    ArrangedBounds(frame, placement)));
            }

            return new ExplorerSnapshot(windows);
        }
        finally
        {
            tabs.ForEach(tab => tab.Dispose());
        }
    }

    public static SavedPlacement? ReadPlacement(nint frame)
    {
        var placement = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        if (!IsWindow(frame) || !GetWindowPlacement(frame, &placement) || placement.showCmd == 0)
        {
            return null;
        }

        return new SavedPlacement(
            (int)placement.showCmd,
            (int)placement.flags,
            placement.ptMinPosition.X,
            placement.ptMinPosition.Y,
            placement.ptMaxPosition.X,
            placement.ptMaxPosition.Y,
            placement.rcNormalPosition.Left,
            placement.rcNormalPosition.Top,
            placement.rcNormalPosition.Right,
            placement.rcNormalPosition.Bottom);
    }

    /// <summary>
    /// The real bounds of a snapped window (Snap, Snap Layouts): its placement still describes
    /// the window from before the snap. Null for any other window.
    /// </summary>
    private static SavedBounds? ArrangedBounds(nint frame, SavedPlacement placement)
    {
        bool arranged;
        try
        {
            arranged = placement.ShowCommand == SW_SHOWNORMAL && IsWindowArranged(frame);
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }

        RECT rect;
        return arranged && GetWindowRect(frame, &rect) ? new SavedBounds(rect.Left, rect.Top, rect.Right, rect.Bottom) : null;
    }

    public static void ApplyPlacement(nint frame, SavedWindow window)
    {
        SavedPlacement saved = window.Placement;
        if (saved.ShowCommand == 0)
        {
            // SW_HIDE: a window nobody could see or close.
            return;
        }

        // WPF_RESTORETOMAXIMIZED only means something for a minimized window; the minimized
        // position is left to Windows.
        var placement = new WINDOWPLACEMENT
        {
            length = (uint)sizeof(WINDOWPLACEMENT),
            flags = saved.ShowCommand == SW_SHOWMINIMIZED ? (uint)saved.Flags & WPF_RESTORETOMAXIMIZED : 0,
            showCmd = (uint)saved.ShowCommand,
            ptMinPosition = new POINT { X = saved.MinX, Y = saved.MinY },
            ptMaxPosition = new POINT { X = saved.MaxX, Y = saved.MaxY },
            rcNormalPosition = new RECT { Left = saved.Left, Top = saved.Top, Right = saved.Right, Bottom = saved.Bottom },
        };

        SetWindowPlacement(frame, &placement);

        // Put back where it was, not snapped again: Windows offers no call for that.
        if (window.Arranged is { } bounds)
        {
            SetWindowPos(frame, 0, bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }
}
