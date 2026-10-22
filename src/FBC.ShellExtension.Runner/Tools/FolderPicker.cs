using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Windows' own folder picker: the Open dialog in folder mode (IFileOpenDialog with
/// FOS_PICKFOLDERS), as Explorer's "Copy to" shows it (ADR-0026).
/// </summary>
internal static unsafe class FolderPicker
{
    private static readonly Guid s_fileOpenDialog = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    private static readonly Guid s_iFileOpenDialog = new("D57C7288-D4AD-4768-BE02-9D969532D960");

    // IFileOpenDialog: IUnknown (0-2), IModalWindow.Show (3), then IFileDialog (4-26).
    private const int ShowSlot = 3;
    private const int SetOptionsSlot = 9;
    private const int GetOptionsSlot = 10;
    private const int SetDefaultFolderSlot = 11;
    private const int SetTitleSlot = 17;
    private const int SetOkButtonLabelSlot = 18;
    private const int GetResultSlot = 20;
    private const int SetClientGuidSlot = 24;

    /// <summary>Asks for a folder.</summary>
    /// <param name="client">
    /// One per tool: Windows remembers the last folder chosen under it and opens there next time.
    /// </param>
    /// <param name="startFolder">Where the picker opens when nothing was chosen under <paramref name="client"/> yet.</param>
    /// <returns>The chosen folder; null when the user cancelled.</returns>
    /// <exception cref="ToolFailedException">The picker could not be shown.</exception>
    public static string? Pick(string title, string okLabel, Guid client, string startFolder)
    {
        using var apartment = new ShellCom.Apartment();
        Guid clsid = s_fileOpenDialog;
        Guid iid = s_iFileOpenDialog;
        nint dialog = 0;
        int hresult = CoCreateInstance(&clsid, 0, CLSCTX_INPROC_SERVER, &iid, &dialog);
        if (hresult < 0 || dialog == 0)
        {
            throw new ToolFailedException(Strings.ToolPickerFailed(ShellCom.Describe(hresult)));
        }

        try
        {
            nint* vtable = *(nint**)dialog;

            // The Open dialog's defaults stay; folder mode and file system folders only are added.
            uint options = 0;
            Check(((delegate* unmanaged<nint, uint*, int>)vtable[GetOptionsSlot])(dialog, &options));
            Check(((delegate* unmanaged<nint, uint, int>)vtable[SetOptionsSlot])(dialog,
                options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOCHANGEDIR));
            Check(((delegate* unmanaged<nint, Guid*, int>)vtable[SetClientGuidSlot])(dialog, &client));

            nint start = ShellCom.ItemFromPath(startFolder, out _);
            if (start != 0)
            {
                // Only when nothing was chosen under this client yet; the dialog keeps its own reference.
                ((delegate* unmanaged<nint, nint, int>)vtable[SetDefaultFolderSlot])(dialog, start);
                ShellCom.Release(start);
            }

            // The texts stay pinned until the dialog is closed.
            fixed (char* titleText = title, okText = okLabel)
            {
                Check(((delegate* unmanaged<nint, char*, int>)vtable[SetTitleSlot])(dialog, titleText));
                Check(((delegate* unmanaged<nint, char*, int>)vtable[SetOkButtonLabelSlot])(dialog, okText));
                hresult = ((delegate* unmanaged<nint, nint, int>)vtable[ShowSlot])(dialog, 0);
            }

            if (hresult == HRESULT_CANCELLED)
            {
                return null;
            }

            Check(hresult);
            nint result = 0;
            Check(((delegate* unmanaged<nint, nint*, int>)vtable[GetResultSlot])(dialog, &result));
            try
            {
                return ShellCom.FileSystemPath(result) ?? throw new ToolFailedException(Strings.ToolPickerFailed("no file system path"));
            }
            finally
            {
                ShellCom.Release(result);
            }
        }
        finally
        {
            ShellCom.Release(dialog);
        }
    }

    private static void Check(int hresult)
    {
        if (hresult < 0)
        {
            throw new ToolFailedException(Strings.ToolPickerFailed(ShellCom.Describe(hresult)));
        }
    }
}
