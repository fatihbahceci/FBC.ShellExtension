using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Commands;

namespace FBC.ShellExtension.ShellExt;

/// <summary>The "Edit FBCTools menu" item: starts the editor next to this DLL (ADR-0007).</summary>
internal static class EditorLauncher
{
    public static string Path => System.IO.Path.Combine(Module.Directory, ProductInfo.EditorFileName);

    /// <summary>
    /// One attribute read of a local file per right-click; without the editor the item is left
    /// out rather than shown and failing.
    /// </summary>
    public static bool IsAvailable => File.Exists(Path);

    public static void Launch()
    {
        string editor = Path;
        ProcessStarter.Start(editor, CommandLine.Quote(editor));
        Diagnostics.Trace("InvokeCommand: started the editor");
    }
}
