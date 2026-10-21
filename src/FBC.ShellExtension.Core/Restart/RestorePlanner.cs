namespace FBC.ShellExtension.Core.Restart;

/// <summary>
/// Decides which saved windows still have to be opened after the restart. Explorer may bring
/// some back by itself; opening those again would duplicate them (ADR-0008).
/// </summary>
public static class RestorePlanner
{
    /// <param name="open">The folders of each window that is open now, one list per window.</param>
    /// <returns>
    /// The saved windows no open window stands for, in the snapshot's order. An open window
    /// stands for a saved one when it shows the same folders, in any order; each open window
    /// stands for one saved window at most.
    /// </returns>
    public static IReadOnlyList<SavedWindow> Missing(ExplorerSnapshot saved, IReadOnlyList<IReadOnlyList<byte[]>> open)
    {
        var unclaimed = open.ToList();
        var missing = new List<SavedWindow>();
        foreach (SavedWindow window in saved.Windows)
        {
            int match = unclaimed.FindIndex(folders => SameFolders(window.Tabs, folders));
            if (match >= 0)
            {
                unclaimed.RemoveAt(match);
            }
            else
            {
                missing.Add(window);
            }
        }

        return missing;
    }

    private static bool SameFolders(IReadOnlyList<SavedTab> tabs, IReadOnlyList<byte[]> folders)
    {
        if (tabs.Count != folders.Count)
        {
            return false;
        }

        var left = folders.ToList();
        foreach (SavedTab tab in tabs)
        {
            int index = left.FindIndex(tab.IsSameFolder);
            if (index < 0)
            {
                return false;
            }

            left.RemoveAt(index);
        }

        return true;
    }
}
