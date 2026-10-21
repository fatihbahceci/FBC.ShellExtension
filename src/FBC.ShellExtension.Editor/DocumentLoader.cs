using System.Diagnostics;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor;

/// <param name="Document">What to edit; null when there is nothing new to edit.</param>
/// <param name="Exit">The user chose to leave the editor.</param>
internal sealed record LoadResult(ConfigDocument? Document, bool Exit);

/// <summary>Opens the config file and, when it cannot be read, lets the user decide what happens to it.</summary>
internal static class DocumentLoader
{
    public static LoadResult Load(string path, IWin32Window? owner)
    {
        try
        {
            return new LoadResult(ConfigDocument.Load(path), false);
        }
        catch (ConfigLoadException ex)
        {
            return AskAboutUnreadable(path, ex.Message, owner);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(owner, Strings.EditorReadFailed(ex.Message), ProductInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return new LoadResult(null, false);
        }
    }

    private static LoadResult AskAboutUnreadable(string path, string detail, IWin32Window? owner)
    {
        var notepad = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedNotepad());
        var startOver = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedStartOver(), Strings.EditorLoadFailedStartOverNote());
        var exit = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedExit());
        var page = new TaskDialogPage
        {
            Caption = ProductInfo.Name,
            Heading = Strings.EditorLoadFailedHeading(),
            Text = detail,
            Icon = TaskDialogIcon.Error,
            Buttons = { notepad, startOver, exit },
            DefaultButton = notepad,
        };

        TaskDialogButton choice = owner is null ? TaskDialog.ShowDialog(page) : TaskDialog.ShowDialog(owner, page);
        if (choice == startOver)
        {
            return new LoadResult(ConfigDocument.StartOver(path), false);
        }

        if (choice == notepad)
        {
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = false })?.Dispose();
            return new LoadResult(null, false);
        }

        // Exit, or the dialog closed some other way.
        return new LoadResult(null, true);
    }
}
