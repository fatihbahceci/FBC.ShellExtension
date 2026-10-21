using System.Diagnostics;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// "Test" (ADR-0007): the menu Explorer would show for chosen items, built from the config in
/// memory, the processes a command would start, and running it, or a built-in tool, through the
/// real runner.
/// </summary>
public partial class PreviewForm : Form
{
    private readonly Func<MenuConfig> _config;
    private readonly Func<int> _issueCount;
    private readonly List<string> _paths = [];

    public PreviewForm(Func<MenuConfig> config, Func<int> issueCount)
    {
        _config = config;
        _issueCount = issueCount;
        InitializeComponent();
        ApplyStrings();
        RefreshMenu();
    }

    private void ApplyStrings()
    {
        Text = Strings.EditorPreviewTitle();
        rightClickLabel.Text = Strings.EditorPreviewRightClickOn();
        itemsRadio.Text = Strings.EditorPreviewItems();
        backgroundRadio.Text = Strings.EditorPreviewBackground();
        addFilesButton.Text = Strings.EditorPreviewAddFiles();
        addFolderButton.Text = Strings.EditorPreviewAddFolder();
        removeButton.Text = Strings.EditorPreviewRemove();
        clearButton.Text = Strings.EditorPreviewClear();
        pathColumn.Text = Strings.EditorPreviewPathColumn();
        kindColumn.Text = Strings.EditorPreviewKindColumn();
        programColumn.Text = Strings.EditorPreviewProgram();
        argumentsColumn.Text = Strings.EditorPreviewArguments();
        folderColumn.Text = Strings.EditorPreviewFolder();
        runButton.Text = Strings.EditorPreviewRun();
        closeButton.Text = Strings.EditorPreviewClose();
    }

    /// <summary>Builds the menu again; the editor calls this after every change.</summary>
    public void RefreshMenu()
    {
        IReadOnlyList<SelectedItem> selection = BuildSelection(out string? problem);
        RefreshSelectionList(selection);

        int[]? selectedPath = PathOf(menuTree.SelectedNode);
        menuTree.BeginUpdate();
        menuTree.Nodes.Clear();
        IReadOnlyList<MenuNode> nodes = selection.Count > 0 ? MenuBuilder.Build(_config().Items, selection) : [];
        if (selection.Count > 0)
        {
            var root = new TreeNode(ProductInfo.Name);
            AddNodes(root.Nodes, nodes);
            if (nodes.Count > 0)
            {
                root.Nodes.Add(new TreeNode(Strings.EditorSeparatorNode()) { ForeColor = SystemColors.GrayText });
            }

            root.Nodes.Add(new TreeNode(Strings.MenuEditConfig()) { ForeColor = SystemColors.GrayText });
            menuTree.Nodes.Add(root);
            root.ExpandAll();
        }

        menuTree.EndUpdate();
        menuTree.SelectedNode = NodeAt(selectedPath);
        ShowProcesses(menuTree.SelectedNode?.Tag as ActionNode);

        hintLabel.Text = problem
            ?? (_issueCount() > 0 ? Strings.EditorPreviewInvalid()
            : nodes.Count == 0 ? Strings.EditorPreviewNoMenu()
            : "");
        hintLabel.ForeColor = _issueCount() > 0 && problem is null ? Color.Firebrick : SystemColors.GrayText;
    }

    private IReadOnlyList<SelectedItem> BuildSelection(out string? problem)
    {
        problem = null;
        if (_paths.Count == 0)
        {
            problem = Strings.EditorPreviewDropHint();
            return [];
        }

        if (backgroundRadio.Checked)
        {
            string[] folders = [.. _paths.Where(Directory.Exists)];
            if (folders.Length != 1)
            {
                problem = Strings.EditorPreviewNeedOneFolder();
                return [];
            }

            return [new SelectedItem(folders[0], ItemKind.Background)];
        }

        return [.. _paths.Select(path => new SelectedItem(path, SelectedItem.Classify(path, Directory.Exists)))];
    }

    private void RefreshSelectionList(IReadOnlyList<SelectedItem> selection)
    {
        selectionList.BeginUpdate();
        selectionList.Items.Clear();
        foreach (string path in _paths)
        {
            SelectedItem? item = selection.FirstOrDefault(i => i.Path == path);
            ItemKind kind = item?.Kind ?? SelectedItem.Classify(path, Directory.Exists);
            selectionList.Items.Add(new ListViewItem([path, KindText(kind)]) { Tag = path });
        }

        selectionList.EndUpdate();
    }

    private static string KindText(ItemKind kind) => kind switch
    {
        ItemKind.File => Strings.EditorKindFile(),
        ItemKind.Folder => Strings.EditorKindFolder(),
        ItemKind.Background => Strings.EditorKindBackground(),
        _ => Strings.EditorKindDrive(),
    };

    private static void AddNodes(TreeNodeCollection target, IReadOnlyList<MenuNode> nodes)
    {
        foreach (MenuNode node in nodes)
        {
            switch (node)
            {
                case ActionNode action:
                    target.Add(new TreeNode(action.Label) { Tag = action });
                    break;
                case SubmenuNode submenu:
                    var child = new TreeNode(submenu.Label);
                    AddNodes(child.Nodes, submenu.Children);
                    target.Add(child);
                    break;
                default:
                    target.Add(new TreeNode(Strings.EditorSeparatorNode()) { ForeColor = SystemColors.GrayText });
                    break;
            }
        }
    }

    private static int[]? PathOf(TreeNode? node)
    {
        var path = new List<int>();
        for (; node is not null; node = node.Parent)
        {
            path.Insert(0, node.Index);
        }

        return path.Count > 0 ? [.. path] : null;
    }

    private TreeNode? NodeAt(int[]? path)
    {
        TreeNodeCollection level = menuTree.Nodes;
        TreeNode? node = null;
        foreach (int index in path ?? [])
        {
            if (index >= level.Count)
            {
                return null;
            }

            node = level[index];
            level = node.Nodes;
        }

        return node;
    }

    private void ShowProcesses(ActionNode? action)
    {
        processList.BeginUpdate();
        processList.Items.Clear();
        runButton.Enabled = action is not null;
        // A built-in tool starts no process; the runner does the work itself (ADR-0015).
        processList.Visible = action is not ToolNode;
        switch (action)
        {
            case CommandNode command:
                IReadOnlyList<PlannedProcess> processes = CommandPlanner.Plan(command.Command, command.Items);
                foreach (PlannedProcess process in processes)
                {
                    processList.Items.Add(new ListViewItem([process.FileName, process.Arguments, process.WorkingDirectory]));
                }

                noteLabel.Text = command.Command.RunsElevated
                    ? Strings.EditorPreviewElevated(processes.Count)
                    : Strings.EditorPreviewProcessCount(processes.Count);
                break;
            case ToolNode tool:
                noteLabel.Text = ToolTexts.Note(tool.Tool);
                break;
            default:
                noteLabel.Text = Strings.EditorPreviewSelectCommand();
                break;
        }

        processList.EndUpdate();
    }

    private void OnMenuAfterSelect(object? sender, TreeViewEventArgs e) => ShowProcesses(e.Node?.Tag as ActionNode);

    private void OnModeChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton { Checked: true })
        {
            RefreshMenu();
        }
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            if (!_paths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                _paths.Add(path);
            }
        }

        RefreshMenu();
    }

    private void OnAddFiles(object? sender, EventArgs e)
    {
        if (filesDialog.ShowDialog(this) == DialogResult.OK)
        {
            AddPaths(filesDialog.FileNames);
        }
    }

    private void OnAddFolder(object? sender, EventArgs e)
    {
        if (folderDialog.ShowDialog(this) == DialogResult.OK)
        {
            AddPaths([folderDialog.SelectedPath]);
        }
    }

    private void OnRemove(object? sender, EventArgs e)
    {
        foreach (ListViewItem item in selectionList.SelectedItems)
        {
            _paths.Remove((string)item.Tag!);
        }

        RefreshMenu();
    }

    private void OnClear(object? sender, EventArgs e)
    {
        _paths.Clear();
        RefreshMenu();
    }

    private void OnSelectionKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            OnRemove(sender, e);
            e.Handled = true;
        }
    }

    private void OnSelectionDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnSelectionDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddPaths(paths);
        }
    }

    /// <summary>Exactly what the menu item does: a job file, then the runner next to the editor.</summary>
    private void OnRun(object? sender, EventArgs e)
    {
        if (menuTree.SelectedNode?.Tag is not ActionNode action)
        {
            return;
        }

        string runner = Path.Combine(AppContext.BaseDirectory, ProductInfo.RunnerFileName);
        if (!File.Exists(runner))
        {
            MessageBox.Show(this, Strings.EditorPreviewRunnerMissing(runner), ProductInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string? job = null;
        try
        {
            job = JobFile.Write(RunJob.For(action));
            var start = new ProcessStartInfo(runner) { UseShellExecute = false };
            start.ArgumentList.Add(JobFile.Argument);
            start.ArgumentList.Add(job);
            Process.Start(start)?.Dispose();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            if (job is not null)
            {
                try
                {
                    File.Delete(job);
                }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
                {
                    // The original failure is the one to report.
                }
            }

            MessageBox.Show(this, Strings.EditorPreviewRunFailed(ex.Message), ProductInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClose(object? sender, EventArgs e) => Close();
}
