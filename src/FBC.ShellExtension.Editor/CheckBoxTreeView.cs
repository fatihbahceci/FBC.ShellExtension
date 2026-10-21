namespace FBC.ShellExtension.Editor;

/// <summary>
/// A tree view whose check boxes stay in step with <see cref="TreeNode.Checked"/>. The native
/// control also toggles a check box on the second click of a double click, and WinForms raises
/// no <see cref="TreeView.AfterCheck"/> for it: the box then shows one state while the node
/// holds the other. That second click is dropped here; the first one has toggled the box.
/// </summary>
public class CheckBoxTreeView : TreeView
{
    private const int WM_LBUTTONDBLCLK = 0x0203;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_LBUTTONDBLCLK && CheckBoxes)
        {
            long position = m.LParam;
            var point = new Point((short)position, (short)(position >> 16));
            if (HitTest(point).Location == TreeViewHitTestLocations.StateImage)
            {
                return;
            }
        }

        base.WndProc(ref m);
    }
}
