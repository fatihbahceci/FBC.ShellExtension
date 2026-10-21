namespace FBC.ShellExtension.Core.Setup;

/// <summary>
/// Where the shell extension registers itself, per user under HKCU\Software\Classes (ADR-0005).
/// The DLL writes these keys; the manager reads them to see which copy Explorer uses.
/// </summary>
public static class RegistrationKeys
{
    /// <summary>Files, folders, the empty area of a folder window, and drive roots.</summary>
    public static IReadOnlyList<string> HandlerClasses { get; } = ["*", "Directory", @"Directory\Background", "Drive"];

    public static string Clsid => ProductInfo.ContextMenuClsid.ToString("B").ToUpperInvariant();

    public static string ClsidKey => $@"Software\Classes\CLSID\{Clsid}";

    /// <summary>Its default value is the path of the registered DLL.</summary>
    public static string InprocServerKey => $@"{ClsidKey}\InprocServer32";

    public static string HandlerKey(string handlerClass) =>
        $@"Software\Classes\{handlerClass}\shellex\ContextMenuHandlers\{ProductInfo.Name}";
}
