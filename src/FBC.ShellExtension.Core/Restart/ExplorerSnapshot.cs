namespace FBC.ShellExtension.Core.Restart;

/// <summary>
/// One tab of a folder window; on Windows 10, the window's only "tab". The ID list is the
/// absolute PIDL as bytes: it names virtual folders (This PC, Home, Recycle Bin) as well as
/// paths, and stays valid across an Explorer restart.
/// </summary>
/// <param name="DisplayName">The parsing name, for messages only.</param>
public sealed record SavedTab(byte[] IdList, string DisplayName)
{
    public bool IsSameFolder(byte[] idList) => IdList.AsSpan().SequenceEqual(idList);
}

/// <summary><c>WINDOWPLACEMENT</c> of a folder window, in workspace coordinates.</summary>
public sealed record SavedPlacement(
    int ShowCommand,
    int Flags,
    int MinX,
    int MinY,
    int MaxX,
    int MaxY,
    int Left,
    int Top,
    int Right,
    int Bottom);

/// <summary>A rectangle in screen coordinates.</summary>
public sealed record SavedBounds(int Left, int Top, int Right, int Bottom);

/// <param name="Tabs">In the order Explorer listed them; the visual order is not available (ADR-0008).</param>
/// <param name="ActiveTab">Index into <paramref name="Tabs"/> of the tab in front.</param>
/// <param name="Arranged">
/// Where a snapped window actually was. For a snapped window the placement still holds its size
/// from before the snap.
/// </param>
public sealed record SavedWindow(IReadOnlyList<SavedTab> Tabs, int ActiveTab, SavedPlacement Placement, SavedBounds? Arranged = null);

/// <param name="Windows">Bottom of the z-order first: opening them in this order leaves the topmost on top.</param>
public sealed record ExplorerSnapshot(IReadOnlyList<SavedWindow> Windows)
{
    public static ExplorerSnapshot Empty { get; } = new([]);

    public int TabCount => Windows.Sum(window => window.Tabs.Count);
}
