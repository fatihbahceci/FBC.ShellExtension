using System.Reflection;

namespace FBC.ShellExtension.Core;

public static class ProductInfo
{
    /// <summary>Label of the root menu item and the name of the per-user folders.</summary>
    public const string Name = "FBCTools";

    /// <summary>
    /// CLSID of the context menu handler. Permanent: every registration points at it, and a
    /// new value would leave the old registrations behind as orphans.
    /// </summary>
    public static readonly Guid ContextMenuClsid = new("6A2B342C-06D2-4E9B-9A0C-D02BF27F8921");

    /// <summary>The runner sits next to the shell extension DLL under this name.</summary>
    public const string RunnerFileName = "FBC.ShellExtension.Runner.exe";

    /// <summary>The config editor sits next to the shell extension DLL under this name.</summary>
    public const string EditorFileName = "FBC.ShellExtension.Editor.exe";

    public const string ShellExtFileName = "FBC.ShellExtension.ShellExt.dll";

    /// <summary>Checks and repairs the installation and the config (Aşama 5), published next to the others.</summary>
    public const string ManagerFileName = "FBC.ShellExtension.Manager.exe";

    /// <summary>The menu FBCTools starts with, shipped next to the programs (ADR-0009).</summary>
    public const string TemplateFileName = "config.template.json";

    /// <summary>The Explorer restarter, published next to the others (ADR-0008).</summary>
    public const string RestarterFileName = "FBC.ShellExtension.Restarter.exe";

    /// <summary>The version of this build, without the commit (ADR-0004).</summary>
    public static Version Version { get; } = typeof(ProductInfo).Assembly.GetName().Version ?? new Version(0, 0);

    /// <summary>Version and commit of this build, such as <c>0.2.0+1a2b3c4</c> (ADR-0004).</summary>
    public static string InformationalVersion { get; } =
        typeof(ProductInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";
}
