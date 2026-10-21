namespace FBC.ShellExtension.Core.Configuration;

/// <summary>Root of the JSON configuration that describes the FBCTools menu.</summary>
public sealed class MenuConfig
{
    /// <summary>
    /// The schema version this build writes. Version 2 adds entry ids (ADR-0009), version 3
    /// built-in tools and <c>enabled</c> (ADR-0015), versions 4 to 7 more tools (ADR-0017 …
    /// ADR-0020). Older documents are still read, and a missing <c>version</c> means this one.
    /// </summary>
    public const int CurrentVersion = 7;

    /// <summary>The oldest schema version this build reads.</summary>
    public const int MinimumVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Entries shown under the root "FBCTools" item, in order.</summary>
    public List<MenuEntry> Items { get; set; } = [];

    /// <summary>
    /// The lowest version that can hold what the entries use: the version of the newest built-in
    /// tool, 3 for an <c>enabled</c> value, 2 for an id, otherwise 1. The editor raises
    /// <see cref="Version"/> to it.
    /// </summary>
    public int RequiredVersion()
    {
        int required = MinimumVersion;
        Visit(Items);
        return required;

        void Visit(IEnumerable<MenuEntry?> entries)
        {
            foreach (MenuEntry? entry in entries)
            {
                if (entry is null)
                {
                    continue;
                }

                if (entry is CommandEntry { Tool: { } tool })
                {
                    required = Math.Max(required, BuiltinTools.SinceVersion(tool));
                }

                if (entry.Enabled is not null)
                {
                    required = Math.Max(required, 3);
                }
                else if (entry.Id is not null)
                {
                    required = Math.Max(required, 2);
                }

                if (entry is SubmenuEntry submenu)
                {
                    Visit(submenu.Items);
                }
            }
        }
    }
}
