using System.Diagnostics;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;

namespace FBC.ShellExtension.Editor;

/// <param name="Document">What to edit; null when there is nothing new to edit.</param>
/// <param name="Exit">The user chose to leave the editor.</param>
/// <param name="Newer">A newer FBCTools wrote the file: nothing on screen may be saved over it (ADR-0038).</param>
internal sealed record LoadResult(ConfigDocument? Document, bool Exit, bool Newer = false);

/// <summary>Opens the config file and, when it cannot be read, lets the user decide what happens to it.</summary>
internal static class DocumentLoader
{
    public static LoadResult Load(string path, IWin32Window? owner)
    {
        try
        {
            return new LoadResult(ConfigDocument.Load(path), false);
        }
        catch (ConfigNewerException ex)
        {
            return AskAboutNewer(path, ex.Message, owner);
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
        var factory = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedFactory(), Strings.EditorLoadFailedFactoryNote());
        var startOver = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedStartOver(), Strings.EditorLoadFailedStartOverNote());
        var exit = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedExit());
        var page = new TaskDialogPage
        {
            Caption = ProductInfo.Name,
            Heading = Strings.EditorLoadFailedHeading(),
            Text = detail,
            Icon = TaskDialogIcon.Error,
            DefaultButton = notepad,
        };

        // The factory menu comes from the manager; without it the choice would only fail.
        page.Buttons.Add(notepad);
        if (ManagerClient.IsAvailable)
        {
            page.Buttons.Add(factory);
        }

        page.Buttons.Add(startOver);
        page.Buttons.Add(exit);

        TaskDialogButton choice = owner is null ? TaskDialog.ShowDialog(page) : TaskDialog.ShowDialog(owner, page);
        if (choice == factory)
        {
            return UseFactoryDefault(path, detail, owner);
        }

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

    /// <summary>
    /// The manager puts the template this build ships with in place of the file, a backup of it
    /// first (ADR-0038); its answer says where the backup went. When it fails, the choices come back.
    /// </summary>
    private static LoadResult UseFactoryDefault(string path, string detail, IWin32Window? owner)
    {
        ManagerResult reset = ManagerClient.Run("reset");
        MessageBox.Show(owner, reset.Output, ProductInfo.Name, MessageBoxButtons.OK,
            reset.ExitCode == ManagerExitCode.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        return reset.ExitCode == ManagerExitCode.Success ? Load(path, owner) : AskAboutUnreadable(path, detail, owner);
    }

    /// <summary>
    /// A newer FBCTools wrote the file: an older editor left open across an install, a downgrade,
    /// a config from another machine (ADR-0038). Neither an empty nor the factory menu is offered,
    /// and nothing on screen is saved over the file; it can still be fixed by hand.
    /// </summary>
    private static LoadResult AskAboutNewer(string path, string detail, IWin32Window? owner)
    {
        var notepad = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedNotepad());
        var exit = new TaskDialogCommandLinkButton(Strings.EditorLoadFailedExit());
        var page = new TaskDialogPage
        {
            Caption = ProductInfo.Name,
            Heading = Strings.EditorConfigNewerHeading(),
            Text = Strings.EditorConfigNewer(detail),
            Icon = TaskDialogIcon.Warning,
            Buttons = { notepad, exit },
            DefaultButton = exit,
        };

        TaskDialogButton choice = owner is null ? TaskDialog.ShowDialog(page) : TaskDialog.ShowDialog(owner, page);
        if (choice == notepad)
        {
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = false })?.Dispose();
            return new LoadResult(null, false, Newer: true);
        }

        return new LoadResult(null, true, Newer: true);
    }
}
