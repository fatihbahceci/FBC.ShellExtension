using System.Diagnostics;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// The menu tree on the left, the selected entry on the right, the validation issues below
/// (ADR-0007). Every edit goes through <see cref="EditHistory"/> first, then the model, then
/// the view is brought up to date from the model.
/// </summary>
public partial class MainForm : Form
{
    private static readonly Color s_issueColor = Color.Firebrick;

    private readonly string _path;
    private readonly EditHistory _history = new();
    private ConfigDocument _document = null!;
    private IReadOnlyList<ConfigIssue> _issues = [];
    private HashSet<MenuEntry> _entriesWithIssues = new(ReferenceEqualityComparer.Instance);
    private MenuEntry? _selected;
    private PreviewForm? _preview;
    private bool _checkingDisk;
    private bool _buildingTree;
    private string? _statusNote;
    private string? _statusTarget;
    private string? _pendingSnapshot;
    private string? _pendingKey;

    public MainForm(string path)
    {
        _path = path;
        InitializeComponent();
        ApplyStrings();
        entryEditor.Changing += key =>
        {
            _pendingSnapshot = ConfigSerializer.Serialize(Config);
            _pendingKey = key;
        };
        entryEditor.Changed += OnEntryEdited;
    }

    private MenuConfig Config => _document.Config;

    private void ApplyStrings()
    {
        var keys = new KeysConverter();
        moveUpItem.ShortcutKeyDisplayString = keys.ConvertToString(Keys.Alt | Keys.Up);
        moveDownItem.ShortcutKeyDisplayString = keys.ConvertToString(Keys.Alt | Keys.Down);
        moveInItem.ShortcutKeyDisplayString = keys.ConvertToString(Keys.Alt | Keys.Right);
        moveOutItem.ShortcutKeyDisplayString = keys.ConvertToString(Keys.Alt | Keys.Left);
        fileMenu.Text = Strings.EditorMenuFile();
        saveItem.Text = saveButton.Text = Strings.EditorMenuSave();
        reloadItem.Text = Strings.EditorMenuReload();
        openFolderItem.Text = Strings.EditorMenuOpenFolder();
        exitItem.Text = Strings.EditorMenuExit();
        editMenu.Text = Strings.EditorMenuEdit();
        undoItem.Text = undoButton.Text = Strings.EditorMenuUndo();
        redoItem.Text = redoButton.Text = Strings.EditorMenuRedo();
        addCommandItem.Text = addCommandButton.Text = ctxAddCommandItem.Text = Strings.EditorMenuAddCommand();
        addSubmenuItem.Text = addSubmenuButton.Text = ctxAddSubmenuItem.Text = Strings.EditorMenuAddSubmenu();
        addSeparatorItem.Text = addSeparatorButton.Text = ctxAddSeparatorItem.Text = Strings.EditorMenuAddSeparator();
        duplicateItem.Text = ctxDuplicateItem.Text = Strings.EditorMenuDuplicate();
        deleteItem.Text = deleteButton.Text = ctxDeleteItem.Text = Strings.EditorMenuDelete();
        deleteItem.ShortcutKeyDisplayString = Strings.EditorDeleteKey();
        moveUpItem.Text = moveUpButton.Text = ctxMoveUpItem.Text = Strings.EditorMenuMoveUp();
        moveDownItem.Text = moveDownButton.Text = ctxMoveDownItem.Text = Strings.EditorMenuMoveDown();
        moveInItem.Text = ctxMoveInItem.Text = Strings.EditorMenuMoveIn();
        moveOutItem.Text = ctxMoveOutItem.Text = Strings.EditorMenuMoveOut();
        testMenu.Text = Strings.EditorMenuTest();
        previewItem.Text = previewButton.Text = Strings.EditorMenuPreview();
        toolsMenu.Text = Strings.EditorMenuTools();
        checkItem.Text = Strings.EditorMenuCheck();
        backupItem.Text = Strings.EditorMenuBackup();
        restoreItem.Text = Strings.EditorMenuRestore();
        resetItem.Text = Strings.EditorMenuReset();
        ffmpegItem.Text = Strings.EditorMenuFfmpeg();
        helpMenu.Text = Strings.EditorMenuHelp();
        aboutItem.Text = Strings.EditorMenuAbout();
        // The toolbar would otherwise answer Alt+letter too, against the fields' own access keys.
        foreach (ToolStripItem item in toolStrip.Items)
        {
            item.Text = item.Text?.Replace("&", "");
        }

        issueLocationColumn.Text = Strings.EditorIssueLocation();
        issueProblemColumn.Text = Strings.EditorIssueProblem();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // A first start: the menu comes from the template, whose Convert items need ffmpeg.
        bool firstStart = !File.Exists(_path);

        // Every start repairs the registration and brings the menu up to the template first
        // (Aşama 5); only what needs the user's attention is shown.
        if (ManagerClient.IsAvailable)
        {
            ManagerResult check = ManagerClient.Run("check");
            if (check.ExitCode != ManagerExitCode.Success)
            {
                ShowManagerResult(check, null);
            }
        }

        if (DocumentLoader.Load(_path, null).Document is not { } document)
        {
            // Closing from Load would leave a half-made window on screen for a moment.
            BeginInvoke(new Action(Close));
            _document = ConfigDocument.StartOver(_path);
            return;
        }

        _document = document;
        RefreshAll(Config.Items.FirstOrDefault());

        if (firstStart && ManagerClient.IsAvailable)
        {
            BeginInvoke(new Action(() => _ = OfferFfmpegAsync(showWhenUsable: false)));
        }
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        CheckDisk();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        entryEditor.CommitPendingEdits();
        if (e.Cancel || _document is null || !_document.IsDirty)
        {
            return;
        }

        DialogResult answer = MessageBox.Show(this, Strings.EditorSaveChanges(Path.GetFileName(_path)), ProductInfo.Name,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        e.Cancel = answer == DialogResult.Cancel || (answer == DialogResult.Yes && !Save());
    }

    // ---- view from model ------------------------------------------------------------------

    private void RefreshAll(MenuEntry? select)
    {
        Revalidate();
        RebuildTree(select);
        RefreshStatus();
        _preview?.RefreshMenu();
    }

    private void Revalidate()
    {
        _issues = ConfigValidator.Validate(Config);
        _entriesWithIssues = new HashSet<MenuEntry>(ReferenceEqualityComparer.Instance);
        foreach (ConfigIssue issue in _issues)
        {
            if (IssueLocator.Resolve(Config, issue.Path).Entry is { } entry)
            {
                _entriesWithIssues.Add(entry);
            }
        }

        issuesList.BeginUpdate();
        issuesList.Items.Clear();
        foreach (ConfigIssue issue in _issues)
        {
            issuesList.Items.Add(new ListViewItem([Describe(issue), issue.Message]) { Tag = issue });
        }

        issuesList.EndUpdate();
    }

    private string Describe(ConfigIssue issue)
    {
        IssueTarget target = IssueLocator.Resolve(Config, issue.Path);
        if (target.Entry is null)
        {
            return Strings.EditorIssueDocument();
        }

        var labels = new List<string>();
        List<MenuEntry> level = Config.Items;
        foreach (int index in MenuTree.PathOf(Config, target.Entry) ?? [])
        {
            MenuEntry entry = level[index];
            labels.Add(NodeText(entry));
            level = (entry as SubmenuEntry)?.Items ?? [];
        }

        string where = string.Join(" › ", labels);
        return target.Member.Length > 0 ? $"{where} ({target.Member})" : where;
    }

    private static string NodeText(MenuEntry entry) => entry switch
    {
        LabeledEntry { Label: var label } when !string.IsNullOrWhiteSpace(label) => label,
        LabeledEntry => Strings.EditorNoLabel(),
        _ => Strings.EditorSeparatorNode(),
    };

    private void RebuildTree(MenuEntry? select)
    {
        // Collapsed submenus are remembered by position: after an undo the entries are new objects.
        var collapsed = new HashSet<string>();
        CollectCollapsed(tree.Nodes, "", collapsed);
        // The scroll position too, so a rebuild does not jump; selecting afterwards still brings
        // the selection into view.
        var top = tree.TopNode?.Tag as MenuEntry;

        _buildingTree = true;
        try
        {
            tree.BeginUpdate();
            tree.Nodes.Clear();
            AddNodes(tree.Nodes, Config.Items, "", collapsed);
        }
        finally
        {
            tree.EndUpdate();
            _buildingTree = false;
        }

        if (top is not null && FindNode(tree.Nodes, top) is { } topNode)
        {
            tree.TopNode = topNode;
        }

        TreeNode? node = select is null ? null : FindNode(tree.Nodes, select);
        tree.SelectedNode = node;
        if (node is null)
        {
            _selected = null;
            BindDetail();
        }
    }

    private static void CollectCollapsed(TreeNodeCollection nodes, string prefix, HashSet<string> collapsed)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            string key = $"{prefix}/{i}";
            if (nodes[i].Nodes.Count > 0 && !nodes[i].IsExpanded)
            {
                collapsed.Add(key);
            }

            CollectCollapsed(nodes[i].Nodes, key, collapsed);
        }
    }

    private void AddNodes(TreeNodeCollection nodes, List<MenuEntry> entries, string prefix, HashSet<string> collapsed)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            MenuEntry entry = entries[i];
            string key = $"{prefix}/{i}";
            // A null item (written by hand) passes the reader; the validator reports it.
            var node = new TreeNode(NodeText(entry)) { Tag = entry, Checked = entry?.IsEnabled != false };
            nodes.Add(node);
            StyleNode(node);
            if (entry is SubmenuEntry submenu)
            {
                AddNodes(node.Nodes, submenu.Items, key, collapsed);
                if (!collapsed.Contains(key))
                {
                    node.Expand();
                }
            }
        }
    }

    /// <summary>Needs the node in the tree already: an entry in an unticked submenu is greyed out too.</summary>
    private void StyleNode(TreeNode node)
    {
        var entry = (MenuEntry)node.Tag!;
        node.ForeColor = _entriesWithIssues.Contains(entry) ? s_issueColor
            : entry is SeparatorEntry || !IsShown(node) ? SystemColors.GrayText
            : tree.ForeColor;
    }

    /// <summary>False for an unticked entry and for everything in an unticked submenu.</summary>
    private static bool IsShown(TreeNode? node) =>
        node is null || ((node.Tag as MenuEntry)?.IsEnabled != false && IsShown(node.Parent));

    private static TreeNode? FindNode(TreeNodeCollection nodes, MenuEntry entry)
    {
        foreach (TreeNode node in nodes)
        {
            if (ReferenceEquals(node.Tag, entry))
            {
                return node;
            }

            if (FindNode(node.Nodes, entry) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private void RestyleNodes(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            string text = NodeText((MenuEntry)node.Tag!);
            if (node.Text != text)
            {
                node.Text = text;
            }

            StyleNode(node);
            RestyleNodes(node.Nodes);
        }
    }

    private void BindDetail()
    {
        detailHost.SuspendLayout();
        if (_selected is LabeledEntry labeled)
        {
            messageLabel.Visible = false;
            entryEditor.Bind(labeled);
            entryEditor.Visible = true;
        }
        else
        {
            entryEditor.Visible = false;
            messageLabel.Text = _selected is SeparatorEntry ? Strings.EditorSeparatorSelected() : Strings.EditorNothingSelected();
            messageLabel.Visible = true;
        }

        detailHost.ResumeLayout(true);
        UpdateActions();
    }

    private void RefreshStatus()
    {
        bool dirty = _document.IsDirty;
        Text = Strings.EditorTitle((dirty ? "*" : "") + Path.GetFileName(_path));
        pathStatus.Text = _statusNote ?? _path;
        pathStatus.ToolTipText = _statusNote is null ? null : _statusTarget;
        stateStatus.Text = dirty ? Strings.EditorStatusUnsaved()
            : File.Exists(_path) ? Strings.EditorStatusSaved()
            : Strings.EditorStatusNew();
        issuesStatus.Text = _issues.Count > 0 ? Strings.EditorStatusIssues(_issues.Count) : Strings.EditorStatusNoIssues();
        issuesStatus.ForeColor = _issues.Count > 0 ? s_issueColor : statusStrip.ForeColor;
        UpdateActions();
    }

    private void UpdateActions()
    {
        bool selected = _selected is not null;
        bool up = selected && MenuTree.CanMoveUp(Config, _selected!);
        bool down = selected && MenuTree.CanMoveDown(Config, _selected!);
        bool into = selected && MenuTree.CanMoveIntoPrevious(Config, _selected!);
        bool outOf = selected && MenuTree.CanMoveOut(Config, _selected!);

        undoItem.Enabled = undoButton.Enabled = _history.CanUndo;
        redoItem.Enabled = redoButton.Enabled = _history.CanRedo;
        duplicateItem.Enabled = ctxDuplicateItem.Enabled = selected;
        deleteItem.Enabled = deleteButton.Enabled = ctxDeleteItem.Enabled = selected;
        moveUpItem.Enabled = moveUpButton.Enabled = ctxMoveUpItem.Enabled = up;
        moveDownItem.Enabled = moveDownButton.Enabled = ctxMoveDownItem.Enabled = down;
        moveInItem.Enabled = ctxMoveInItem.Enabled = into;
        moveOutItem.Enabled = ctxMoveOutItem.Enabled = outOf;
    }

    // ---- edits -----------------------------------------------------------------------------

    private void OnEntryEdited()
    {
        RaiseVersion();
        // A keystroke that changes nothing, such as a space the list parsing drops, is no step.
        if (_pendingSnapshot is not null && _pendingSnapshot != ConfigSerializer.Serialize(Config))
        {
            _history.Record(_pendingSnapshot, _pendingKey);
        }

        _pendingSnapshot = null;
        _statusNote = null;
        RefreshInPlace();
    }

    /// <summary>The view after an edit that leaves the tree's shape alone: no rebuild, so the selection and scroll stay.</summary>
    private void RefreshInPlace()
    {
        Revalidate();
        RestyleNodes(tree.Nodes);
        RefreshStatus();
        _preview?.RefreshMenu();
    }

    /// <summary>
    /// A structural edit: one undo step when the config really changed, then the whole view
    /// from the model. <paramref name="change"/> returns the entry to select afterwards.
    /// </summary>
    private void Apply(Func<MenuEntry?> change)
    {
        entryEditor.CommitPendingEdits();
        string before = ConfigSerializer.Serialize(Config);
        MenuEntry? select = change();
        RaiseVersion();
        if (ConfigSerializer.Serialize(Config) != before)
        {
            _history.Record(before);
        }

        _statusNote = null;
        RefreshAll(select);
    }

    /// <summary>
    /// A built-in tool or an unticked entry needs a newer version than an older file has. Every
    /// edit raises it in the same undo step, so that the edit does not turn into a validation
    /// issue; it is never lowered. Undo and reload leave the version as the snapshot has it.
    /// </summary>
    private void RaiseVersion() => Config.Version = Math.Max(Config.Version, Config.RequiredVersion());

    private void Add(MenuEntry entry)
    {
        Apply(() =>
        {
            MenuTree.InsertAfter(Config, _selected, entry);
            return entry;
        });

        if (entry is LabeledEntry)
        {
            entryEditor.FocusLabel();
        }
    }

    private void OnAddCommand(object? sender, EventArgs e) =>
        Add(new CommandEntry { Label = Strings.EditorNewCommandLabel(), Command = new CommandDefinition() });

    private void OnAddSubmenu(object? sender, EventArgs e) =>
        Add(new SubmenuEntry { Label = Strings.EditorNewSubmenuLabel() });

    private void OnAddSeparator(object? sender, EventArgs e) => Add(new SeparatorEntry());

    private void OnDuplicate(object? sender, EventArgs e)
    {
        if (_selected is { } entry)
        {
            Apply(() => MenuTree.Duplicate(Config, entry));
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_selected is not { } entry)
        {
            return;
        }

        Apply(() =>
        {
            if (MenuTree.Locate(Config, entry) is not { } location)
            {
                return entry;
            }

            // Then the entry that takes its place, the one above it, or its submenu.
            MenuEntry? next = location.Index + 1 < location.Siblings.Count ? location.Siblings[location.Index + 1]
                : location.Index > 0 ? location.Siblings[location.Index - 1]
                : location.Parent;
            MenuTree.Remove(Config, entry);
            return next;
        });
        tree.Focus();
    }

    private void MoveSelected(Func<MenuConfig, MenuEntry, bool> move)
    {
        if (_selected is { } entry)
        {
            Apply(() =>
            {
                move(Config, entry);
                return entry;
            });
        }
    }

    private void OnMoveUp(object? sender, EventArgs e) => MoveSelected(MenuTree.MoveUp);

    private void OnMoveDown(object? sender, EventArgs e) => MoveSelected(MenuTree.MoveDown);

    private void OnMoveIn(object? sender, EventArgs e) => MoveSelected(MenuTree.MoveIntoPrevious);

    private void OnMoveOut(object? sender, EventArgs e) => MoveSelected(MenuTree.MoveOut);

    /// <summary>
    /// A ticked entry is written without <c>enabled</c>; only an unticked one carries it, as false.
    /// One undo step like <see cref="Apply"/>, but the tree already shows the box as the user left
    /// it, so it is restyled instead of rebuilt: the selection and the scroll position stay, and
    /// the second click of a double click still lands on the same box.
    /// </summary>
    private void SetEnabled(MenuEntry entry, bool enabled)
    {
        entryEditor.CommitPendingEdits();
        string before = ConfigSerializer.Serialize(Config);
        entry.Enabled = enabled ? null : false;
        RaiseVersion();
        if (ConfigSerializer.Serialize(Config) != before)
        {
            _history.Record(before);
        }

        _statusNote = null;
        RefreshInPlace();
    }

    private void OnUndo(object? sender, EventArgs e) => Step(_history.Undo);

    private void OnRedo(object? sender, EventArgs e) => Step(_history.Redo);

    private void Step(Func<MenuConfig, MenuConfig?> step)
    {
        entryEditor.CommitPendingEdits();
        int[]? path = _selected is null ? null : MenuTree.PathOf(Config, _selected);
        if (step(Config) is not { } config)
        {
            return;
        }

        _document.Config = config;
        _statusNote = null;
        RefreshAll(path is null ? null : MenuTree.At(Config, path));
    }

    private void Select(MenuEntry? entry)
    {
        if (entry is not null && FindNode(tree.Nodes, entry) is { } node)
        {
            tree.SelectedNode = node;
        }
    }

    // ---- tree ------------------------------------------------------------------------------

    private void OnTreeAfterSelect(object? sender, TreeViewEventArgs e)
    {
        _history.Seal();
        _selected = e.Node?.Tag as MenuEntry;
        BindDetail();
    }

    private void OnTreeAfterCheck(object? sender, TreeViewEventArgs e)
    {
        // Any change but the tree being built is the user's: a click, Space, or UI Automation's
        // toggle, which arrives as TreeViewAction.Unknown.
        if (_buildingTree || e.Node is not { Tag: MenuEntry entry } node)
        {
            return;
        }

        SetEnabled(entry, node.Checked);
    }

    private void OnTreeNodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            tree.SelectedNode = e.Node;
        }
    }

    private void OnTreeMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e) => UpdateActions();

    // Delete and the Alt+arrow moves are tree keys, not menu shortcuts: as shortcuts they would
    // also fire in a text box or a combo box, where Alt+Down opens the list.
    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        Action<object?, EventArgs>? command = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.None, Keys.Delete) => OnDelete,
            (Keys.Alt, Keys.Up) => OnMoveUp,
            (Keys.Alt, Keys.Down) => OnMoveDown,
            (Keys.Alt, Keys.Right) => OnMoveIn,
            (Keys.Alt, Keys.Left) => OnMoveOut,
            _ => null,
        };

        if (command is not null)
        {
            command(sender, e);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F2 && _selected is LabeledEntry)
        {
            entryEditor.FocusLabel();
            e.Handled = true;
        }
    }

    private void OnTreeItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode { Tag: MenuEntry })
        {
            tree.DoDragDrop(e.Item, DragDropEffects.Move);
        }
    }

    private void OnTreeDragOver(object? sender, DragEventArgs e) =>
        e.Effect = DropTarget(e) is not null ? DragDropEffects.Move : DragDropEffects.None;

    private void OnTreeDragDrop(object? sender, DragEventArgs e)
    {
        if (DropTarget(e) is ({ } entry, var parent, int index))
        {
            Apply(() =>
            {
                MenuTree.Move(Config, entry, parent, index);
                return entry;
            });
        }
    }

    /// <summary>
    /// Where a drop would put the dragged entry: the upper half of a node means before it, the
    /// lower half after it, and the middle of a submenu node means at the end of that submenu.
    /// </summary>
    private (MenuEntry Entry, SubmenuEntry? Parent, int Index)? DropTarget(DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode { Tag: MenuEntry dragged })
        {
            return null;
        }

        Point point = tree.PointToClient(new Point(e.X, e.Y));
        TreeNode? over = tree.GetNodeAt(point);
        if (over?.Tag is not MenuEntry target)
        {
            return MenuTree.CanMove(Config, dragged, null) ? (dragged, null, Config.Items.Count) : null;
        }

        if (ReferenceEquals(target, dragged) || MenuTree.Locate(Config, target) is not { } location)
        {
            return null;
        }

        int offset = point.Y - over.Bounds.Top;
        int height = Math.Max(over.Bounds.Height, 1);
        (SubmenuEntry? parent, int index) = target is SubmenuEntry submenu && offset > height / 4 && offset < height * 3 / 4
            ? (submenu, submenu.Items.Count)
            : (location.Parent, offset < height / 2 ? location.Index : location.Index + 1);

        return MenuTree.CanMove(Config, dragged, parent) ? (dragged, parent, index) : null;
    }

    private void OnIssueActivate(object? sender, EventArgs e)
    {
        entryEditor.CommitPendingEdits();
        if (issuesList.SelectedItems.Count == 0 || issuesList.SelectedItems[0].Tag is not ConfigIssue issue)
        {
            return;
        }

        IssueTarget target = IssueLocator.Resolve(Config, issue.Path);
        if (target.Entry is null)
        {
            return;
        }

        Select(target.Entry);
        // The box for "enabled" is in the tree.
        if (target.Entry is LabeledEntry && target.Member != "enabled")
        {
            entryEditor.FocusMember(target.Member);
        }
        else
        {
            tree.Focus();
        }
    }

    // ---- file ------------------------------------------------------------------------------

    private bool Save()
    {
        entryEditor.CommitPendingEdits();
        _history.Seal();
        if (_issues.Count > 0 && Ask(Strings.EditorSaveWithIssues(_issues.Count), MessageBoxButtons.YesNo) != DialogResult.Yes)
        {
            return false;
        }

        if (_document.ChangedOnDisk() && Ask(Strings.EditorOverwriteChanged(), MessageBoxButtons.YesNo) != DialogResult.Yes)
        {
            return false;
        }

        if (_document.HasComments && _document.BacksUpOnSave
            && Ask(Strings.EditorCommentsWarning(), MessageBoxButtons.OKCancel) != DialogResult.OK)
        {
            return false;
        }

        try
        {
            string? backup = _document.Save(DateTime.Now);
            _statusNote = backup is null ? null : Strings.EditorStatusBackup(Path.GetFileName(backup));
            _statusTarget = backup;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, Strings.EditorSaveFailed(ex.Message), ProductInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        RefreshStatus();
        return true;
    }

    private DialogResult Ask(string text, MessageBoxButtons buttons) =>
        MessageBox.Show(this, text, ProductInfo.Name, buttons, MessageBoxIcon.Question);

    private void OnSave(object? sender, EventArgs e) => Save();

    private void OnReload(object? sender, EventArgs e)
    {
        entryEditor.CommitPendingEdits();
        if (!_document.IsDirty || Ask(Strings.EditorReloadDiscard(), MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            Reload();
        }
    }

    private void Reload()
    {
        int[]? path = _selected is null ? null : MenuTree.PathOf(Config, _selected);
        LoadResult result = DocumentLoader.Load(_path, this);
        if (result.Exit)
        {
            // Not saved over the file the user is fixing by hand: closing still asks.
            _document.KeepDespiteUnreadableFile();
            BeginInvoke(new Action(Close));
            return;
        }

        if (result.Document is not { } document)
        {
            // The document in memory stays; the file that could not be read is copied aside
            // before a save overwrites it (ADR-0007).
            _document.KeepDespiteUnreadableFile();
            return;
        }

        _document = document;
        _history.Clear();
        _statusNote = null;
        RefreshAll(path is null ? null : MenuTree.At(Config, path));
    }

    /// <summary>Someone else changed the file: offer to reload it, once per change.</summary>
    private void CheckDisk()
    {
        if (_checkingDisk || _document is null || !_document.ChangedOnDisk())
        {
            return;
        }

        _checkingDisk = true;
        try
        {
            string question = _document.IsDirty ? Strings.EditorChangedOnDiskDirty() : Strings.EditorChangedOnDisk();
            if (Ask(question, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Reload();
            }
            else
            {
                _document.AcceptDiskState();
            }
        }
        finally
        {
            _checkingDisk = false;
        }
    }

    private void OnOpenFolder(object? sender, EventArgs e) => ShowInExplorer(_path);

    /// <summary>The path in the status bar: the config, or the backup a save has just made.</summary>
    private void OnPathStatusClick(object? sender, EventArgs e) =>
        ShowInExplorer(_statusNote is not null && _statusTarget is not null ? _statusTarget : _path);

    /// <summary>Opens the file's folder in Explorer with the file selected, or just the folder when the file does not exist yet.</summary>
    private static void ShowInExplorer(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string folder = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(folder);
        string arguments = File.Exists(fullPath) ? $"/select,\"{fullPath}\"" : $"\"{folder}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = false })?.Dispose();
    }

    private void OnExit(object? sender, EventArgs e) => Close();

    private void OnPreview(object? sender, EventArgs e)
    {
        entryEditor.CommitPendingEdits();
        if (_preview is null || _preview.IsDisposed)
        {
            _preview = new PreviewForm(() => Config, () => _issues.Count);
            _preview.Show(this);
        }
        else
        {
            _preview.Activate();
        }
    }

    // ---- tools (the manager) ------------------------------------------------------------------

    private void ShowManagerResult(ManagerResult result, IWin32Window? owner)
    {
        MessageBoxIcon icon = result.ExitCode switch
        {
            ManagerExitCode.Success => MessageBoxIcon.Information,
            ManagerExitCode.Attention => MessageBoxIcon.Warning,
            _ => MessageBoxIcon.Error,
        };
        MessageBox.Show(owner, result.Output, ProductInfo.Name, MessageBoxButtons.OK, icon);
    }

    private void OnCheckInstallation(object? sender, EventArgs e)
    {
        if (ConfirmReplace())
        {
            RunReplacing("check");
        }
    }

    /// <summary>
    /// A manager command that may rewrite the config. The editor's own "changed on disk" question
    /// is held back meanwhile; the document is reloaded only when the file really changed, so a
    /// failed command keeps what is on screen.
    /// </summary>
    private void RunReplacing(params string[] arguments)
    {
        _checkingDisk = true;
        try
        {
            ManagerResult result = ManagerClient.Run(arguments);
            if (_document.ChangedOnDisk())
            {
                Reload();
            }

            ShowManagerResult(result, this);
        }
        finally
        {
            _checkingDisk = false;
        }
    }

    private void OnBackup(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = Strings.EditorConfigFilter(),
            FileName = Path.GetFileName(ConfigLocation.BackupPath(_path, DateTime.Now)),
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ShowManagerResult(ManagerClient.Run("backup", dialog.FileName), this);
        }
    }

    private void OnRestore(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = Strings.EditorConfigFilter() };
        if (dialog.ShowDialog(this) != DialogResult.OK || !ConfirmReplace())
        {
            return;
        }

        RunReplacing("restore", dialog.FileName);
    }

    private void OnReset(object? sender, EventArgs e)
    {
        if (Ask(Strings.EditorResetConfirm(), MessageBoxButtons.OKCancel) != DialogResult.OK || !ConfirmReplace())
        {
            return;
        }

        RunReplacing("reset");
    }

    /// <summary>Before the manager rewrites the file: unsaved edits are saved, dropped, or the command is called off.</summary>
    private bool ConfirmReplace()
    {
        entryEditor.CommitPendingEdits();
        if (!_document.IsDirty)
        {
            return true;
        }

        DialogResult answer = MessageBox.Show(this, Strings.EditorReplaceUnsaved(), ProductInfo.Name,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer == DialogResult.No || (answer == DialogResult.Yes && Save());
    }

    private async void OnFfmpeg(object? sender, EventArgs e) => await OfferFfmpegAsync(showWhenUsable: true);

    /// <summary>
    /// Checks ffmpeg; if it is missing or lacks an encoder, offers the two install routes (ADR-0011).
    /// Nothing is downloaded or installed unless the user picks one.
    /// </summary>
    private async Task OfferFfmpegAsync(bool showWhenUsable)
    {
        ManagerResult status = ManagerClient.Run("ffmpeg");
        if (status.ExitCode == ManagerExitCode.Success)
        {
            if (showWhenUsable)
            {
                ShowManagerResult(status, this);
            }

            return;
        }

        var winget = new TaskDialogCommandLinkButton(Strings.EditorFfmpegWinget(), Strings.EditorFfmpegWingetNote());
        var download = new TaskDialogCommandLinkButton(Strings.EditorFfmpegDownload(), Strings.EditorFfmpegDownloadNote());
        var notNow = new TaskDialogCommandLinkButton(Strings.EditorFfmpegNotNow());
        var addToPath = new TaskDialogVerificationCheckBox(Strings.EditorFfmpegAddToPath());
        // An ffmpeg that was found but lacks encoders comes before anything winget installs for
        // the user; only the download, which the runner prefers, replaces it (ADR-0011).
        bool shadowed = !status.Output.Contains(Strings.FfmpegNotFound(), StringComparison.Ordinal);
        string text = shadowed
            ? $"{status.Output}\n\n{Strings.EditorFfmpegShadowed()}\n\n{Strings.EditorFfmpegLicence()}"
            : $"{status.Output}\n\n{Strings.EditorFfmpegLicence()}";
        var page = new TaskDialogPage
        {
            Caption = Strings.EditorFfmpegTitle(),
            Heading = Strings.EditorFfmpegHeading(),
            Text = text,
            Icon = TaskDialogIcon.Information,
            Buttons = { winget, download, notNow },
            DefaultButton = shadowed ? download : winget,
            Verification = addToPath,
        };

        TaskDialogButton choice = TaskDialog.ShowDialog(this, page);
        if (choice != winget && choice != download)
        {
            return;
        }

        ManagerResult result = await RunWithProgressAsync(choice == winget
            ? ["ffmpeg", "install", "winget"]
            : ["ffmpeg", "install", "download"]);

        if (choice == download && addToPath.Checked && result.ExitCode != ManagerExitCode.Failed)
        {
            ManagerResult path = ManagerClient.Run("ffmpeg", "path", "add");
            result = result with { Output = $"{result.Output}\n{path.Output}" };
        }

        ShowManagerResult(result, this);
    }

    /// <summary>A long manager command off the UI thread, with the form disabled and a progress window up.</summary>
    private async Task<ManagerResult> RunWithProgressAsync(string[] arguments)
    {
        using var progress = new ProgressForm(Strings.EditorFfmpegWorking());
        progress.Show(this);
        Enabled = false;
        try
        {
            CancellationToken cancellation = progress.Cancellation;
            return await Task.Run(() => ManagerClient.Run(TimeSpan.FromMinutes(40), cancellation, arguments));
        }
        finally
        {
            Enabled = true;
            progress.Close();
        }
    }

    private void OnAbout(object? sender, EventArgs e) =>
        MessageBox.Show(this, Strings.EditorAbout(ProductInfo.InformationalVersion, _path), ProductInfo.Name,
            MessageBoxButtons.OK, MessageBoxIcon.Information);
}
