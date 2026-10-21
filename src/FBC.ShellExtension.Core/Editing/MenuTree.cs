using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Editing;

/// <summary>Where an entry sits: the list that holds it, its index there and the submenu that owns the list.</summary>
public sealed record EntryLocation(List<MenuEntry> Siblings, int Index, SubmenuEntry? Parent);

/// <summary>
/// The structural edits the editor offers. Entries are found by reference, so the caller keeps
/// pointing at the same objects across edits. Every method leaves the tree unchanged when the
/// edit is not possible.
/// </summary>
public static class MenuTree
{
    public static EntryLocation? Locate(MenuConfig config, MenuEntry entry) => Locate(config.Items, null, entry);

    private static EntryLocation? Locate(List<MenuEntry> entries, SubmenuEntry? parent, MenuEntry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i], entry))
            {
                return new EntryLocation(entries, i, parent);
            }

            if (entries[i] is SubmenuEntry submenu && Locate(submenu.Items, submenu, entry) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Indexes from the root list down to the entry; null when it is not in the tree.</summary>
    public static int[]? PathOf(MenuConfig config, MenuEntry entry)
    {
        var path = new List<int>();
        return Collect(config.Items, entry, path) ? [.. path] : null;
    }

    private static bool Collect(List<MenuEntry> entries, MenuEntry entry, List<int> path)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            path.Add(i);
            if (ReferenceEquals(entries[i], entry)
                || (entries[i] is SubmenuEntry submenu && Collect(submenu.Items, entry, path)))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
        }

        return false;
    }

    /// <summary>The entry at a path from <see cref="PathOf"/>; null when the tree no longer has it.</summary>
    public static MenuEntry? At(MenuConfig config, IReadOnlyList<int> path)
    {
        List<MenuEntry> entries = config.Items;
        for (int depth = 0; depth < path.Count; depth++)
        {
            int index = path[depth];
            if (index < 0 || index >= entries.Count)
            {
                return null;
            }

            MenuEntry entry = entries[index];
            if (depth == path.Count - 1)
            {
                return entry;
            }

            if (entry is not SubmenuEntry submenu)
            {
                return null;
            }

            entries = submenu.Items;
        }

        return null;
    }

    /// <summary>Puts the entry right after <paramref name="anchor"/>, or at the end of the root list when there is none.</summary>
    public static void InsertAfter(MenuConfig config, MenuEntry? anchor, MenuEntry entry)
    {
        if (anchor is not null && Locate(config, anchor) is { } location)
        {
            location.Siblings.Insert(location.Index + 1, entry);
        }
        else
        {
            config.Items.Add(entry);
        }
    }

    public static bool Remove(MenuConfig config, MenuEntry entry)
    {
        if (Locate(config, entry) is not { } location)
        {
            return false;
        }

        location.Siblings.RemoveAt(location.Index);
        return true;
    }

    /// <summary>
    /// Inserts a deep copy right after the entry and returns it. The copy is the user's own:
    /// it carries no template ids, which must stay unique (ADR-0009).
    /// </summary>
    public static MenuEntry? Duplicate(MenuConfig config, MenuEntry entry)
    {
        if (Locate(config, entry) is not { } location)
        {
            return null;
        }

        MenuEntry copy = Clone(entry);
        ClearIds(copy);
        location.Siblings.Insert(location.Index + 1, copy);
        return copy;
    }

    private static void ClearIds(MenuEntry entry)
    {
        entry.Id = null;
        if (entry is SubmenuEntry submenu)
        {
            submenu.Items.ForEach(ClearIds);
        }
    }

    /// <summary>A deep copy, made through the same serializer that writes the file.</summary>
    public static MenuEntry Clone(MenuEntry entry)
    {
        string json = ConfigSerializer.Serialize(new MenuConfig { Items = [entry] });
        return ConfigSerializer.Deserialize(json).Items[0];
    }

    /// <summary>
    /// Up within the list, or, from the top of a submenu, out of it to just above that submenu.
    /// So moving an entry step by step never stops at a submenu's edge.
    /// </summary>
    public static bool CanMoveUp(MenuConfig config, MenuEntry entry) =>
        Locate(config, entry) is { } location && (location.Index > 0 || location.Parent is not null);

    /// <summary>Down within the list, or, from the bottom of a submenu, out of it to just below that submenu.</summary>
    public static bool CanMoveDown(MenuConfig config, MenuEntry entry) =>
        Locate(config, entry) is { } location && (location.Index < location.Siblings.Count - 1 || location.Parent is not null);

    public static bool MoveUp(MenuConfig config, MenuEntry entry)
    {
        if (Locate(config, entry) is not { } location)
        {
            return false;
        }

        if (location.Index > 0)
        {
            return Swap(location, -1);
        }

        if (location.Parent is not { } parent || Locate(config, parent) is not { } parentLocation)
        {
            return false;
        }

        location.Siblings.RemoveAt(location.Index);
        parentLocation.Siblings.Insert(parentLocation.Index, entry);
        return true;
    }

    public static bool MoveDown(MenuConfig config, MenuEntry entry)
    {
        if (Locate(config, entry) is not { } location)
        {
            return false;
        }

        return location.Index < location.Siblings.Count - 1 ? Swap(location, +1) : MoveOut(config, entry);
    }

    private static bool Swap(EntryLocation location, int offset)
    {
        int other = location.Index + offset;
        (location.Siblings[location.Index], location.Siblings[other]) = (location.Siblings[other], location.Siblings[location.Index]);
        return true;
    }

    /// <summary>True when the entry right above is a submenu the entry can become the last child of.</summary>
    public static bool CanMoveIntoPrevious(MenuConfig config, MenuEntry entry) =>
        Locate(config, entry) is { Index: > 0 } location && location.Siblings[location.Index - 1] is SubmenuEntry;

    public static bool MoveIntoPrevious(MenuConfig config, MenuEntry entry)
    {
        if (Locate(config, entry) is not { Index: > 0 } location
            || location.Siblings[location.Index - 1] is not SubmenuEntry target)
        {
            return false;
        }

        location.Siblings.RemoveAt(location.Index);
        target.Items.Add(entry);
        return true;
    }

    public static bool CanMoveOut(MenuConfig config, MenuEntry entry) => Locate(config, entry) is { Parent: not null };

    /// <summary>Takes the entry out of its submenu and puts it right after that submenu.</summary>
    public static bool MoveOut(MenuConfig config, MenuEntry entry)
    {
        if (Locate(config, entry) is not { Parent: { } parent } location
            || Locate(config, parent) is not { } parentLocation)
        {
            return false;
        }

        location.Siblings.RemoveAt(location.Index);
        parentLocation.Siblings.Insert(parentLocation.Index + 1, entry);
        return true;
    }

    /// <summary>
    /// Whether the entry may go into <paramref name="target"/> (null: the root list). A submenu
    /// cannot go into itself or into one of its own descendants.
    /// </summary>
    public static bool CanMove(MenuConfig config, MenuEntry entry, SubmenuEntry? target)
    {
        if (Locate(config, entry) is null)
        {
            return false;
        }

        if (target is null)
        {
            return true;
        }

        return Locate(config, target) is not null && !ReferenceEquals(entry, target) && !Contains(entry, target);
    }

    private static bool Contains(MenuEntry ancestor, MenuEntry entry) =>
        ancestor is SubmenuEntry submenu
        && submenu.Items.Any(child => ReferenceEquals(child, entry) || Contains(child, entry));

    /// <summary>
    /// Moves the entry so that it ends up at <paramref name="index"/> of the target list
    /// (null: the root list), counted as the list looks before the move; drag and drop gives
    /// exactly that. An index past the end appends.
    /// </summary>
    public static bool Move(MenuConfig config, MenuEntry entry, SubmenuEntry? target, int index)
    {
        if (!CanMove(config, entry, target) || Locate(config, entry) is not { } location)
        {
            return false;
        }

        List<MenuEntry> destination = target?.Items ?? config.Items;
        if (ReferenceEquals(destination, location.Siblings) && location.Index < index)
        {
            index--;
        }

        location.Siblings.RemoveAt(location.Index);
        destination.Insert(Math.Clamp(index, 0, destination.Count), entry);
        return true;
    }
}
