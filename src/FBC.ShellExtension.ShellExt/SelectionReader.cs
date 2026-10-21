using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.ShellExt.Interop;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>Turns what IShellExtInit.Initialize receives into <see cref="SelectedItem"/>s.</summary>
internal static unsafe class SelectionReader
{
    private const int MaxLongPath = 32768;

    /// <remarks>
    /// Selected files, folders and drives arrive as a data object carrying CF_HDROP. A click on
    /// a folder's empty area arrives as the folder's ID list with no data object. Items without
    /// a file system path (Control Panel, a phone, the inside of a zip) give an empty selection.
    /// </remarks>
    public static IReadOnlyList<SelectedItem> Read(nint pidlFolder, nint dataObject)
    {
        if (dataObject != 0)
        {
            return ReadDrop(dataObject);
        }

        if (pidlFolder != 0 && GetPath(pidlFolder) is { } folder)
        {
            return [new SelectedItem(folder, ItemKind.Background)];
        }

        return [];
    }

    private static List<SelectedItem> ReadDrop(nint dataObject)
    {
        var format = new FORMATETC
        {
            cfFormat = CF_HDROP,
            dwAspect = DVASPECT_CONTENT,
            lindex = -1,
            tymed = TYMED_HGLOBAL,
        };
        STGMEDIUM medium = default;

        // IDataObject::GetData is the fourth vtable slot, after QueryInterface, AddRef and Release.
        var getData = (delegate* unmanaged<nint, FORMATETC*, STGMEDIUM*, int>)(*(void***)dataObject)[3];
        if (getData(dataObject, &format, &medium) < 0)
        {
            return [];
        }

        try
        {
            if (medium.tymed != TYMED_HGLOBAL || medium.unionmember == 0)
            {
                return [];
            }

            nint drop = medium.unionmember;
            uint count = DragQueryFile(drop, uint.MaxValue, null, 0);
            var items = new List<SelectedItem>((int)count);
            char[] buffer = new char[260];
            for (uint i = 0; i < count; i++)
            {
                uint length = DragQueryFile(drop, i, null, 0);
                if (length == 0)
                {
                    continue;
                }

                if (buffer.Length <= length)
                {
                    buffer = new char[length + 1];
                }

                fixed (char* p = buffer)
                {
                    length = DragQueryFile(drop, i, p, (uint)buffer.Length);
                }

                string path = new(buffer, 0, (int)length);
                items.Add(new SelectedItem(path, Classify(path)));
            }

            return items;
        }
        finally
        {
            ReleaseStgMedium(&medium);
        }
    }

    // One attribute read per item. The item is one Explorer has just listed, so this is
    // expected to be answered from cache; a slow network share is the case to watch.
    private static ItemKind Classify(string path) => SelectedItem.Classify(path, static candidate =>
    {
        uint attributes = GetFileAttributes(candidate);
        return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
    });

    private static string? GetPath(nint pidl)
    {
        char[] buffer = new char[MaxLongPath];
        fixed (char* p = buffer)
        {
            return SHGetPathFromIDListEx(pidl, p, (uint)buffer.Length, 0)
                ? new string(p)
                : null;
        }
    }
}
