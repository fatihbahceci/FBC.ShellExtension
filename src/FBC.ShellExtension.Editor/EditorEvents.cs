namespace FBC.ShellExtension.Editor;

/// <summary>
/// How the detail editors report an edit. <see cref="Changing"/> comes before the model is
/// touched, so the form can record an undo step; the key groups consecutive edits of one text
/// field into that step, null starts a new one (EditHistory).
/// </summary>
internal interface IEntryFieldEditor
{
    event Action<string?>? Changing;

    event Action? Changed;
}
