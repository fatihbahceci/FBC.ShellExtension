using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tests;

internal static class TestItems
{
    public static SelectedItem File(string path) => new(path, ItemKind.File);

    public static SelectedItem Folder(string path) => new(path, ItemKind.Folder);

    public static SelectedItem Background(string path) => new(path, ItemKind.Background);

    public static SelectedItem Drive(string path) => new(path, ItemKind.Drive);

    public static Func<string, string?> Env(params (string Name, string Value)[] variables) =>
        name => variables.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)).Value;
}
