using static FBC.ShellExtension.TestHost.Native;

namespace FBC.ShellExtension.TestHost;

/// <summary>The two arguments IShellExtInit.Initialize receives, built the way Explorer builds them.</summary>
internal sealed unsafe class Selection : IDisposable
{
    private readonly List<nint> _pidls = [];

    public nint FolderPidl { get; private set; }

    public nint DataObject { get; private set; }

    /// <summary>A click on the empty area of <paramref name="folder"/>: its ID list, no data object.</summary>
    public static Selection ForBackground(string folder)
    {
        var selection = new Selection();
        selection.FolderPidl = selection.Parse(folder);
        return selection;
    }

    /// <summary>
    /// Selected items, which must share a folder as an Explorer selection does. The data object
    /// comes from the folder's own IShellFolder.GetUIObjectOf, which is where Explorer gets it.
    /// </summary>
    public static Selection ForItems(IReadOnlyList<string> paths)
    {
        var selection = new Selection();
        try
        {
            selection.BuildDataObject(paths);
            return selection;
        }
        catch
        {
            selection.Dispose();
            throw;
        }
    }

    private void BuildDataObject(IReadOnlyList<string> paths)
    {
        nint parentFolder = 0;
        var children = new nint[paths.Count];
        try
        {
            for (int i = 0; i < paths.Count; i++)
            {
                nint pidl = Parse(paths[i]);
                Guid iid = IID_IShellFolder;
                nint folder;
                nint child;
                Program.Check(SHBindToParent(pidl, &iid, &folder, &child), $"SHBindToParent({paths[i]})");
                children[i] = child;

                nint parentPidl = ILClone(pidl);
                ILRemoveLastID(parentPidl);
                if (i == 0)
                {
                    parentFolder = folder;
                    FolderPidl = parentPidl;
                    _pidls.Add(parentPidl);
                    continue;
                }

                bool sameParent = ILIsEqual(parentPidl, FolderPidl);
                ILFree(parentPidl);
                Release(folder);
                if (!sameParent)
                {
                    throw new HResultException($"{paths[i]} is not in the same folder as {paths[0]}; an Explorer selection always is.");
                }
            }

            // IShellFolder::GetUIObjectOf is the eleventh vtable slot.
            var getUIObjectOf = (delegate* unmanaged<nint, nint, uint, nint*, Guid*, uint*, nint*, int>)Slot(parentFolder, 10);
            Guid iidDataObject = IID_IDataObject;
            nint dataObject;
            fixed (nint* apidl = children)
            {
                Program.Check(getUIObjectOf(parentFolder, 0, (uint)children.Length, apidl, &iidDataObject, null, &dataObject), "IShellFolder.GetUIObjectOf");
            }

            DataObject = dataObject;
        }
        finally
        {
            if (parentFolder != 0)
            {
                Release(parentFolder);
            }
        }
    }

    private nint Parse(string path)
    {
        Program.Check(SHParseDisplayName(Path.GetFullPath(path), 0, out nint pidl, 0, out _), $"SHParseDisplayName({path})");
        _pidls.Add(pidl);
        return pidl;
    }

    public void Dispose()
    {
        if (DataObject != 0)
        {
            Release(DataObject);
            DataObject = 0;
        }

        foreach (nint pidl in _pidls)
        {
            ILFree(pidl);
        }

        _pidls.Clear();
    }
}
