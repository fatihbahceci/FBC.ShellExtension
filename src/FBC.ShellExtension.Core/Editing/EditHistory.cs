using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Editing;

/// <summary>
/// Undo and redo as whole-document snapshots: a config is small, and a snapshot cannot get out
/// of step with the tree the way a list of inverse operations can.
/// </summary>
public sealed class EditHistory(int capacity = 200)
{
    private readonly List<string> _undo = [];
    private readonly List<string> _redo = [];
    private string? _lastKey;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Call before a change, with the document as it still is. Consecutive changes with the same
    /// <paramref name="coalesceKey"/>, such as keystrokes in one field, become one undo step;
    /// null always starts a new step.
    /// </summary>
    public void Record(MenuConfig before, string? coalesceKey = null) =>
        Record(ConfigSerializer.Serialize(before), coalesceKey);

    /// <summary>
    /// The same with a snapshot from <see cref="ConfigSerializer.Serialize"/>, for a caller that
    /// records only once it knows the change really changed something.
    /// </summary>
    public void Record(string before, string? coalesceKey = null)
    {
        if (coalesceKey is not null && coalesceKey == _lastKey && _undo.Count > 0)
        {
            return;
        }

        _lastKey = coalesceKey;
        Push(_undo, before);
        _redo.Clear();
    }

    /// <summary>Ends the current coalescing run, so the next change starts a new step.</summary>
    public void Seal() => _lastKey = null;

    public MenuConfig? Undo(MenuConfig current) => Step(_undo, _redo, current);

    public MenuConfig? Redo(MenuConfig current) => Step(_redo, _undo, current);

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _lastKey = null;
    }

    private MenuConfig? Step(List<string> from, List<string> to, MenuConfig current)
    {
        if (from.Count == 0)
        {
            return null;
        }

        string snapshot = from[^1];
        from.RemoveAt(from.Count - 1);
        Push(to, ConfigSerializer.Serialize(current));
        _lastKey = null;
        return ConfigSerializer.Deserialize(snapshot);
    }

    private void Push(List<string> stack, string snapshot)
    {
        stack.Add(snapshot);
        if (stack.Count > capacity)
        {
            stack.RemoveAt(0);
        }
    }
}
