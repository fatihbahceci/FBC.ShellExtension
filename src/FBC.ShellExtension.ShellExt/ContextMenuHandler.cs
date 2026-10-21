using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.ShellExt.Interop;
using static FBC.ShellExtension.ShellExt.Interop.NativeMethods;

namespace FBC.ShellExtension.ShellExt;

/// <summary>
/// One instance per right-click. Explorer calls Initialize with the selection, then
/// QueryContextMenu, then InvokeCommand if one of our items was chosen. Every method runs on
/// Explorer's UI thread, so nothing here waits on anything but a local file.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ContextMenuHandler : IShellExtInit, IContextMenu
{
    private IReadOnlyList<SelectedItem> _selection = [];

    // Index = command offset from idCmdFirst, as InvokeCommand receives it.
    private readonly List<ActionNode> _commands = [];

    // Offset of "Edit FBCTools menu", which comes after the commands; -1 when it is not shown.
    private int _editOffset = -1;

    public int Initialize(nint pidlFolder, nint pdtobj, nint hkeyProgId)
    {
        try
        {
            _selection = SelectionReader.Read(pidlFolder, pdtobj);
            string firstKind = _selection.Count > 0 ? _selection[0].Kind.ToString() : "-";
            Diagnostics.Trace($"Initialize: {_selection.Count} item(s), first kind {firstKind}, folder pidl {pidlFolder != 0}, data object {pdtobj != 0}");
            return S_OK;
        }
        catch (Exception ex)
        {
            _selection = [];
            return Diagnostics.Fail(nameof(Initialize), ex);
        }
    }

    public int QueryContextMenu(nint hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags)
    {
        _commands.Clear();
        _editOffset = -1;
        try
        {
            if ((uFlags & CMF_DEFAULTONLY) != 0 || _selection.Count == 0 || idCmdFirst > idCmdLast)
            {
                return S_OK;
            }

            ConfigSnapshot snapshot = ConfigCache.Get();
            IReadOnlyList<MenuNode> nodes = snapshot.Status == ConfigStatus.Valid
                ? MenuBuilder.Build(snapshot.Config!.Items, _selection)
                : [];

            // With the editor installed the menu is always there, so the way to change it is
            // too (ADR-0007).
            bool editor = EditorLauncher.IsAvailable;
            if (snapshot.Status == ConfigStatus.Valid && nodes.Count == 0 && !editor)
            {
                return S_OK;
            }

            nint root = CreatePopupMenu();
            if (root == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            uint idsUsed;
            try
            {
                if (snapshot.Status == ConfigStatus.Valid)
                {
                    AppendNodes(root, nodes, idCmdFirst, idCmdLast);
                    idsUsed = (uint)_commands.Count;
                }
                else
                {
                    // A greyed item has an id but no command behind it; InvokeCommand rejects it.
                    string text = snapshot.Status == ConfigStatus.Missing ? Strings.MenuNoConfig() : Strings.MenuConfigInvalid();
                    Check(AppendMenu(root, MF_STRING | MF_GRAYED, idCmdFirst, text));
                    idsUsed = 1;
                }

                if (editor && idCmdFirst + idsUsed <= idCmdLast)
                {
                    if (snapshot.Status != ConfigStatus.Valid || nodes.Count > 0)
                    {
                        Check(AppendMenu(root, MF_SEPARATOR, 0, null));
                    }

                    Check(AppendMenu(root, MF_STRING, idCmdFirst + idsUsed, Strings.MenuEditConfig()));
                    _editOffset = (int)idsUsed;
                    idsUsed++;
                }

                Check(InsertMenu(hmenu, indexMenu, MF_BYPOSITION | MF_POPUP | MF_STRING, (nuint)root, ProductInfo.Name));
            }
            catch
            {
                DestroyMenu(root);
                throw;
            }

            // MAKE_HRESULT(SEVERITY_SUCCESS, 0, n): the number of command ids taken.
            return (int)idsUsed;
        }
        catch (Exception ex)
        {
            _commands.Clear();
            _editOffset = -1;
            return Diagnostics.Fail(nameof(QueryContextMenu), ex);
        }
    }

    public int InvokeCommand(CMINVOKECOMMANDINFO* pici)
    {
        try
        {
            if (pici == null)
            {
                return E_INVALIDARG;
            }

            // A value above 0xFFFF is a verb string, not one of our offsets.
            nuint verb = (nuint)pici->lpVerb;
            if ((verb >> 16) != 0)
            {
                return E_FAIL;
            }

            if (_editOffset >= 0 && verb == (nuint)_editOffset)
            {
                EditorLauncher.Launch();
                return S_OK;
            }

            if (verb >= (nuint)_commands.Count)
            {
                return E_FAIL;
            }

            RunnerLauncher.Launch(_commands[(int)verb]);
            return S_OK;
        }
        catch (Exception ex)
        {
            return Diagnostics.Fail(nameof(InvokeCommand), ex);
        }
    }

    public int GetCommandString(nuint idCmd, uint uType, uint* pReserved, byte* pszName, uint cchMax) => E_NOTIMPL;

    private void AppendNodes(nint menu, IReadOnlyList<MenuNode> nodes, uint idCmdFirst, uint idCmdLast)
    {
        foreach (MenuNode node in nodes)
        {
            switch (node)
            {
                case ActionNode command:
                    uint id = idCmdFirst + (uint)_commands.Count;
                    if (id > idCmdLast)
                    {
                        Diagnostics.Trace("QueryContextMenu: out of command ids, the rest of the menu is left out");
                        return;
                    }

                    Check(AppendMenu(menu, MF_STRING, id, command.Label));
                    _commands.Add(command);
                    break;

                case SubmenuNode submenu:
                    nint child = CreatePopupMenu();
                    if (child == 0)
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }

                    try
                    {
                        AppendNodes(child, submenu.Children, idCmdFirst, idCmdLast);
                        Check(AppendMenu(menu, MF_POPUP | MF_STRING, (nuint)child, submenu.Label));
                    }
                    catch
                    {
                        DestroyMenu(child);
                        throw;
                    }

                    break;

                case SeparatorNode:
                    Check(AppendMenu(menu, MF_SEPARATOR, 0, null));
                    break;
            }
        }
    }

    private static void Check(bool succeeded)
    {
        if (!succeeded)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }
}
