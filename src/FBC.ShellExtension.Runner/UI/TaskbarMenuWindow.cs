using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Shared;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.UI;

/// <summary>
/// The taskbar menu (ADR-0043): started by the pinned launcher with <see cref="ProductInfo.TaskbarMenuArgument"/>,
/// it opens the commands marked "Show in taskbar" at the cursor, on the work area's side of the
/// taskbar, and runs the one chosen as the runner runs any job, with nothing selected.
/// </summary>
internal static unsafe class TaskbarMenuWindow
{
    // 0 is what the menu returns when it is dismissed.
    private const int FirstId = 1;

    public static int Run()
    {
        var actions = new List<ActionNode>();
        nint menu = CreatePopupMenu();
        if (menu == 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        int chosen;
        int editId;
        try
        {
            editId = Fill(menu, actions);
            chosen = Show(menu);
        }
        finally
        {
            // Frees the submenus too; their bitmaps are MenuBitmaps' to keep.
            DestroyMenu(menu);
        }

        if (chosen == 0)
        {
            return 0;
        }

        if (chosen == editId)
        {
            return StartEditor();
        }

        int index = chosen - FirstId;
        return index >= 0 && index < actions.Count ? Program.Run(RunJob.For(actions[index]), withoutSelection: true) : 0;
    }

    /// <summary>The menu's items; returns the id of "Edit FBCTools menu…", or -1 when the editor is not there.</summary>
    private static int Fill(nint menu, List<ActionNode> actions)
    {
        ConfigStatus status = Load(out MenuConfig? config);
        IReadOnlyList<MenuNode> nodes = config is null ? [] : MenuBuilder.BuildForTaskbar(config.Items);
        int iconSize = 0;
        Append(menu, nodes, actions, ref iconSize);

        if (nodes.Count == 0)
        {
            string note = status switch
            {
                ConfigStatus.Missing => Strings.MenuNoConfig(),
                ConfigStatus.Invalid => Strings.MenuConfigInvalid(),
                _ => Strings.TaskbarMenuEmpty(),
            };
            Check(AppendMenuW(menu, MF_STRING | MF_GRAYED, 0x7FFF, note));
        }

        string editor = Path.Combine(AppContext.BaseDirectory, ProductInfo.EditorFileName);
        if (!File.Exists(editor))
        {
            return -1;
        }

        int editId = FirstId + actions.Count;
        Check(AppendMenuW(menu, MF_SEPARATOR, 0, null));
        Check(AppendMenuW(menu, MF_STRING, (nuint)editId, Strings.MenuEditConfig()));
        return editId;
    }

    private enum ConfigStatus
    {
        Valid,
        Missing,
        Invalid,
    }

    private static ConfigStatus Load(out MenuConfig? config)
    {
        config = null;
        string path = ConfigLocation.DefaultPath;
        if (!File.Exists(path))
        {
            return ConfigStatus.Missing;
        }

        try
        {
            MenuConfig read = ConfigSerializer.Load(path);
            if (ConfigValidator.Validate(read).Count > 0)
            {
                return ConfigStatus.Invalid;
            }

            config = read;
            return ConfigStatus.Valid;
        }
        catch (Exception ex) when (ex is ConfigLoadException or IOException or UnauthorizedAccessException)
        {
            return ConfigStatus.Invalid;
        }
    }

    private static void Append(nint menu, IReadOnlyList<MenuNode> nodes, List<ActionNode> actions, ref int iconSize)
    {
        foreach (MenuNode node in nodes)
        {
            switch (node)
            {
                case ActionNode action:
                    Check(AppendMenuW(menu, MF_STRING, (nuint)(FirstId + actions.Count), action.Label));
                    actions.Add(action);
                    SetIcon(menu, action.Icon, ref iconSize);
                    break;

                case SubmenuNode submenu:
                    nint child = CreatePopupMenu();
                    if (child == 0)
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }

                    try
                    {
                        Append(child, submenu.Children, actions, ref iconSize);
                        Check(AppendMenuW(menu, MF_POPUP | MF_STRING, (nuint)child, submenu.Label));
                    }
                    catch
                    {
                        DestroyMenu(child);
                        throw;
                    }

                    SetIcon(menu, submenu.Icon, ref iconSize);
                    break;

                case SeparatorNode:
                    Check(AppendMenuW(menu, MF_SEPARATOR, 0, null));
                    break;
            }
        }
    }

    /// <summary>The icon of the item just appended (ADR-0041); one that cannot be drawn leaves the item without one.</summary>
    private static void SetIcon(nint menu, byte[]? icon, ref int iconSize)
    {
        if (icon is null)
        {
            return;
        }

        if (iconSize == 0)
        {
            iconSize = MenuBitmaps.IconSize();
        }

        nint bitmap = MenuBitmaps.For(icon, iconSize, out _);
        if (bitmap != 0)
        {
            var info = new MENUITEMINFOW { cbSize = (uint)sizeof(MENUITEMINFOW), fMask = MIIM_BITMAP, hbmpItem = bitmap };
            SetMenuItemInfoW(menu, (uint)(GetMenuItemCount(menu) - 1), true, &info);
        }
    }

    /// <summary>
    /// Opens the menu at the cursor and waits for a choice. Clicked on the taskbar, the menu opens
    /// against the taskbar's inner edge, as its own menus do. The taskbar's window gives that edge,
    /// also when it hides itself (the work area is then the whole monitor); without it, a cursor
    /// outside the work area does. Opened from the keyboard (Win+number), it opens at the cursor.
    /// </summary>
    private static int Show(nint menu)
    {
        // A window of this thread to own the menu. The menu closes on a click outside it only
        // when that window is in the foreground; the launcher passed that right on.
        nint owner = CreateWindowExW(WS_EX_TOOLWINDOW, "STATIC", null, WS_POPUP, 0, 0, 0, 0, 0, 0, GetModuleHandleW(0), 0);
        if (owner == 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        try
        {
            POINT cursor;
            GetCursorPos(&cursor);
            var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
            GetMonitorInfoW(MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST), &info);
            RECT work = info.rcWork;

            uint flags = TPM_RETURNCMD | TPM_NONOTIFY | TPM_RIGHTBUTTON;
            int x = cursor.x, y = cursor.y;
            RECT screen = info.rcMonitor;
            if (TaskbarAt(cursor) is { } bar)
            {
                // The taskbar's own window: found when it hides itself too, and on a second
                // monitor. The menu opens against its inner edge.
                if (bar.right - bar.left >= bar.bottom - bar.top)
                {
                    bool bottom = bar.top > (screen.top + screen.bottom) / 2;
                    y = bottom ? bar.top : bar.bottom;
                    flags |= bottom ? TPM_BOTTOMALIGN : TPM_TOPALIGN;
                }
                else
                {
                    bool right = bar.left > (screen.left + screen.right) / 2;
                    x = right ? bar.left : bar.right;
                    flags |= right ? TPM_RIGHTALIGN : TPM_LEFTALIGN;
                }
            }
            else if (cursor.y >= work.bottom)
            {
                y = work.bottom;
                flags |= TPM_BOTTOMALIGN;
            }
            else if (cursor.y < work.top)
            {
                y = work.top;
                flags |= TPM_TOPALIGN;
            }
            else if (cursor.x >= work.right)
            {
                x = work.right;
                flags |= TPM_RIGHTALIGN;
            }
            else if (cursor.x < work.left)
            {
                x = work.left;
                flags |= TPM_LEFTALIGN;
            }

            SetForegroundWindow(owner);
            int chosen = TrackPopupMenuEx(menu, flags, x, y, owner, 0);

            // The documented way to let the menu finish closing before the window goes.
            PostMessageW(owner, WM_NULL, 0, 0);
            return chosen;
        }
        finally
        {
            DestroyWindow(owner);
        }
    }

    /// <summary>The rectangle of the taskbar under the cursor (the main one or a second monitor's); null when the cursor is not on one.</summary>
    private static RECT? TaskbarAt(POINT cursor)
    {
        foreach (string className in new[] { "Shell_TrayWnd", "Shell_SecondaryTrayWnd" })
        {
            for (nint bar = FindWindowExW(0, 0, className, null); bar != 0; bar = FindWindowExW(0, bar, className, null))
            {
                RECT rect;
                if (GetWindowRect(bar, &rect) && cursor.x >= rect.left && cursor.x < rect.right && cursor.y >= rect.top && cursor.y < rect.bottom)
                {
                    return rect;
                }
            }
        }

        return null;
    }

    private static int StartEditor()
    {
        string editor = Path.Combine(AppContext.BaseDirectory, ProductInfo.EditorFileName);
        try
        {
            // The editor's window may come to the front: this process was the foreground one.
            AllowSetForegroundWindow(ASFW_ANY);
            Process.Start(new ProcessStartInfo(editor) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory })?.Dispose();
            return 0;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException)
        {
            MessageBoxW(0, Strings.ToolEditorFailed(ex.Message), ProductInfo.Name, MB_ICONERROR);
            return 1;
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
