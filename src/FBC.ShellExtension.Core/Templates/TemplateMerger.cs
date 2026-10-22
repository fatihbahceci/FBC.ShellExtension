using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Templates;

public enum TemplateAction
{
    /// <summary>There was no config; the template became it.</summary>
    Created,

    /// <summary>The user had not changed the config since the last update; the new template replaced it.</summary>
    Replaced,

    /// <summary>The user had changed the config; entries new in the template were added to it.</summary>
    Merged,

    /// <summary>Nothing to do. Also the first run over a config this program did not write: it is left as it is.</summary>
    UpToDate,

    /// <summary>A newer FBCTools applied its template last; this older one leaves the config to it.</summary>
    ManagedByNewer,

    /// <summary>Adding the new entries would have made the menu invalid; the config was left as it is.</summary>
    Skipped,
}

/// <summary>The template itself cannot be read.</summary>
public sealed class TemplateLoadException(string message, Exception? innerException) : Exception(message, innerException);

/// <param name="ConfigText">What to write as the config; null when it stays as it is.</param>
/// <param name="AddedIds">Template ids added to a changed config.</param>
public sealed record TemplateOutcome(TemplateAction Action, string? ConfigText, TemplateState State, IReadOnlyList<string> AddedIds);

/// <summary>
/// Brings a user's config up to a new template without overwriting their changes (ADR-0009).
/// Pure: the caller reads and writes the files, and backs up a config before a merge writes it.
/// </summary>
public static class TemplateMerger
{
    /// <summary>
    /// The ids the templates of each older config version had. A config copied from a template
    /// keeps its <c>version</c>, so one that comes without a state still tells which template it
    /// came from (ADR-0016). Version 1 templates had no ids; the version 2 ones, 0.5.2 to 0.6.2,
    /// all had the same; so did the version 3 ones, 0.7.x, and the versions 4, 5 and 6 ones,
    /// 0.8.0, 0.9.0 and 0.10.0; the version 7 ones, 0.11.0 to 0.12.x, the version 8 ones, 0.13.x,
    /// the version 9 ones, 0.14.x, the version 10 ones, 0.15.x, the version 11 ones, 0.16.x, and the
    /// version 12 ones, 0.17.x, too. A new config version adds the ids of the last template
    /// written in the old one.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, string[]> TemplateIdsByVersion = new Dictionary<int, string[]>
    {
        [2] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin", "separator-1",
            "text-files", "count-lines", "copy-paths",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [3] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-content", "separator-1",
            "text-files", "count-lines",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [4] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-content",
            "file-tools", "checksum", "verify-checksum", "file-tools-separator", "split", "join", "separator-1",
            "text-files", "count-lines",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [5] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-internet-path", "copy-content",
            "file-tools", "duplicate", "file-tools-checksum-separator", "checksum", "verify-checksum",
            "file-tools-separator", "split", "join", "file-tools-dll-separator", "register-dll", "unregister-dll",
            "folder-tools", "folder-size", "separator-1",
            "text-files", "count-lines", "append-clipboard",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [6] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-internet-path", "copy-title", "copy-content",
            "file-tools", "duplicate", "file-tools-checksum-separator", "checksum", "verify-checksum",
            "file-tools-separator", "split", "join", "file-tools-dll-separator", "run-with-parameters", "register-dll", "unregister-dll",
            "folder-tools", "folder-size",
            "organize", "pack-into-folder", "new-folder", "unpack-folder", "organize-separator", "delete-empty-folders", "delete-permanently",
            "separator-1", "text-files", "count-lines", "append-clipboard",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [7] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-internet-path", "copy-title", "copy-content",
            "file-tools", "duplicate", "file-tools-checksum-separator", "checksum", "verify-checksum",
            "file-tools-separator", "split", "join", "file-tools-dll-separator", "run-with-parameters", "register-dll", "unregister-dll",
            "folder-tools", "folder-size",
            "organize", "pack-into-folder", "new-folder", "unpack-folder", "organize-separator", "delete-empty-folders", "delete-permanently",
            "separator-1", "text-files", "count-lines", "append-clipboard", "send-to-wikijs",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [8] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-internet-path", "copy-title", "copy-content",
            "file-tools", "duplicate", "file-tools-checksum-separator", "checksum", "verify-checksum",
            "file-tools-separator", "split", "join", "file-tools-dll-separator", "run-with-parameters", "register-dll", "unregister-dll",
            "folder-tools", "folder-size", "folder-tools-icon-separator", "change-folder-icon", "restore-folder-icon",
            "organize", "copy-to-folder", "move-to-folder", "create-link", "organize-transfer-separator",
            "pack-into-folder", "new-folder", "unpack-folder", "join-folders", "organize-separator", "delete-empty-folders", "delete-permanently",
            "separator-1", "text-files", "count-lines", "append-clipboard", "send-to-wikijs",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [9] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin",
            "copy", "copy-path", "copy-name", "copy-unc-path", "copy-internet-path", "copy-title", "copy-content",
            "file-tools", "duplicate", "file-tools-checksum-separator", "checksum", "verify-checksum",
            "file-tools-separator", "split", "join", "file-tools-dll-separator", "run-with-parameters", "register-dll", "unregister-dll",
            "folder-tools", "folder-size", "folder-tools-icon-separator", "change-folder-icon", "restore-folder-icon",
            "organize", "copy-to-folder", "move-to-folder", "create-link", "organize-transfer-separator",
            "pack-into-folder", "new-folder", "unpack-folder", "join-folders", "organize-into-folders",
            "organize-properties-separator", "attributes", "change-time",
            "organize-separator", "delete-empty-folders", "advanced-delete", "delete-permanently",
            "separator-1", "text-files", "count-lines", "append-clipboard", "send-to-wikijs",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [10] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin", "copy", "copy-path", "copy-name",
            "copy-unc-path", "copy-internet-path", "copy-title", "copy-content", "file-tools", "duplicate",
            "find-replace", "file-tools-checksum-separator", "checksum", "verify-checksum", "file-tools-separator",
            "split", "join", "file-tools-dll-separator", "run-with-parameters", "register-dll", "unregister-dll",
            "folder-tools", "folder-size", "find-replace-in-folders", "folder-tools-icon-separator",
            "change-folder-icon", "restore-folder-icon", "organize", "copy-to-folder", "move-to-folder",
            "create-link", "organize-transfer-separator", "advanced-rename", "pack-into-folder", "new-folder",
            "unpack-folder", "join-folders", "organize-into-folders", "organize-properties-separator", "attributes",
            "change-time", "organize-separator", "delete-empty-folders", "advanced-delete", "delete-permanently",
            "separator-1", "text-files", "count-lines", "append-clipboard", "send-to-wikijs", "convert", "to-mp3",
            "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [11] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin", "copy", "copy-path", "copy-name",
            "copy-unc-path", "copy-internet-path", "copy-title", "copy-content", "file-tools", "duplicate",
            "find-replace", "file-tools-checksum-separator", "checksum", "verify-checksum", "file-tools-separator",
            "split", "join", "file-tools-dll-separator", "run-with-parameters", "register-dll", "unregister-dll",
            "folder-tools", "folder-size", "find-replace-in-folders", "folder-tools-icon-separator",
            "change-folder-icon", "restore-folder-icon", "organize", "copy-to-folder", "move-to-folder",
            "create-link", "organize-transfer-separator", "advanced-rename", "pack-into-folder", "new-folder",
            "unpack-folder", "join-folders", "organize-into-folders", "organize-properties-separator", "attributes",
            "change-time", "organize-separator", "delete-empty-folders", "advanced-delete", "delete-locked",
            "delete-permanently", "separator-1", "text-files", "count-lines", "append-clipboard", "send-to-wikijs",
            "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
        [12] =
        [
            "open-in-notepad", "command-prompt-here", "command-prompt-here-admin", "copy", "copy-path", "copy-name",
            "copy-unc-path", "copy-internet-path", "copy-title", "copy-content", "file-tools", "duplicate",
            "find-replace", "file-tools-crypto-separator", "encrypt", "decrypt", "file-tools-checksum-separator",
            "checksum", "verify-checksum", "file-tools-separator", "split", "join", "file-tools-dll-separator",
            "run-with-parameters", "register-dll", "unregister-dll", "folder-tools", "folder-size",
            "find-replace-in-folders", "folder-tools-crypto-separator", "encrypt-folder", "decrypt-folder",
            "folder-tools-icon-separator", "change-folder-icon", "restore-folder-icon", "organize", "copy-to-folder",
            "move-to-folder", "create-link", "organize-transfer-separator", "advanced-rename", "pack-into-folder",
            "new-folder", "unpack-folder", "join-folders", "organize-into-folders", "organize-properties-separator",
            "attributes", "change-time", "organize-separator", "delete-empty-folders", "advanced-delete",
            "delete-locked", "delete-permanently", "separator-1", "text-files", "count-lines", "append-clipboard",
            "send-to-wikijs", "convert", "to-mp3", "to-mp4", "convert-separator", "to-jpg", "to-png",
        ],
    };

    /// <param name="version">The FBCTools version applying the template; recorded, and compared with the last one.</param>
    /// <exception cref="TemplateLoadException">The template cannot be read.</exception>
    /// <exception cref="ConfigLoadException">A changed config cannot be read.</exception>
    public static TemplateOutcome Apply(string templateText, string? configText, TemplateState? state, Version version)
    {
        MenuConfig template = ReadTemplate(templateText);
        List<string> templateIds = [.. Ids(template.Items)];
        string templateHash = TemplateState.Hash(templateText);
        string appliedBy = version.ToString();

        if (state?.AppliedBy is { } previous && Version.TryParse(previous, out Version? last) && last > version)
        {
            return new TemplateOutcome(TemplateAction.ManagedByNewer, null, state, []);
        }

        if (configText is null)
        {
            return Written(TemplateAction.Created, templateText, templateHash, templateIds, appliedBy);
        }

        string configHash = TemplateState.Hash(configText);
        if (state is null)
        {
            // A config this program did not write: a copy, a hand-made one, or one made before the
            // state was kept. Copied from an older template, it is offered what came since; any
            // other is left alone, and the template's entries count as already offered.
            if (configHash != templateHash && EarlierTemplateIds(configText, template.Version) is { } earlier)
            {
                state = new TemplateState { AppliedIds = [.. earlier] };
            }
            else
            {
                var adopted = new TemplateState
                {
                    ConfigHash = configHash == templateHash ? configHash : null,
                    TemplateHash = templateHash,
                    AppliedIds = templateIds,
                    AppliedBy = appliedBy,
                };
                return new TemplateOutcome(TemplateAction.UpToDate, null, adopted, []);
            }
        }

        if (state.ConfigHash == configHash)
        {
            return state.TemplateHash == templateHash
                ? new TemplateOutcome(TemplateAction.UpToDate, null, state, [])
                : Written(TemplateAction.Replaced, templateText, templateHash, Union(state.AppliedIds, templateIds), appliedBy);
        }

        MenuConfig config = ConfigSerializer.Deserialize(configText);
        int issuesBefore = ConfigValidator.Validate(config).Count;
        var offered = new HashSet<string>(state.AppliedIds, StringComparer.OrdinalIgnoreCase);
        var present = new HashSet<string>(Ids(config.Items), StringComparer.OrdinalIgnoreCase);
        var added = new List<string>();
        AddNew(template.Items, null, config, offered, present, added);

        var merged = new TemplateState
        {
            ConfigHash = null,
            TemplateHash = templateHash,
            AppliedIds = Union(state.AppliedIds, templateIds),
            AppliedBy = appliedBy,
        };

        if (added.Count == 0)
        {
            return new TemplateOutcome(TemplateAction.UpToDate, null, merged, []);
        }

        config.Version = Math.Max(config.Version, MenuConfig.CurrentVersion);

        // A merge must never be what breaks the menu; then the user's config stays as it is.
        if (ConfigValidator.Validate(config).Count > issuesBefore)
        {
            return new TemplateOutcome(TemplateAction.Skipped, null, state, []);
        }

        return new TemplateOutcome(TemplateAction.Merged, ConfigSerializer.Serialize(config), merged, added);
    }

    /// <summary>
    /// The ids of the older template a config without a state was copied from: its version is
    /// older than the template's and it still has at least one of that version's ids. Null for a
    /// hand-made or unreadable config, or one of the template's own version.
    /// </summary>
    private static string[]? EarlierTemplateIds(string configText, int templateVersion)
    {
        MenuConfig config;
        try
        {
            config = ConfigSerializer.Deserialize(configText);
        }
        catch (ConfigLoadException)
        {
            // An unreadable config is the editor's to sort out (ADR-0007); it is left as it is.
            return null;
        }

        if (config.Version >= templateVersion || !TemplateIdsByVersion.TryGetValue(config.Version, out string[]? earlier))
        {
            return null;
        }

        var present = new HashSet<string>(Ids(config.Items), StringComparer.OrdinalIgnoreCase);
        return earlier.Any(present.Contains) ? earlier : null;
    }

    private static MenuConfig ReadTemplate(string text)
    {
        try
        {
            return ConfigSerializer.Deserialize(text);
        }
        catch (ConfigLoadException ex)
        {
            throw new TemplateLoadException(ex.Message, ex);
        }
    }

    private static TemplateOutcome Written(TemplateAction action, string text, string templateHash, List<string> ids, string appliedBy) =>
        new(action, text, new TemplateState { ConfigHash = TemplateState.Hash(text), TemplateHash = templateHash, AppliedIds = ids, AppliedBy = appliedBy }, []);

    private static List<string> Union(IEnumerable<string> first, IEnumerable<string> second) =>
        [.. first.Concat(second).Distinct(StringComparer.OrdinalIgnoreCase)];

    private static IEnumerable<string> Ids(IEnumerable<MenuEntry?> entries)
    {
        // A null entry passes the JSON reader (the validator reports it).
        foreach (MenuEntry? entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(entry.Id))
            {
                yield return entry.Id;
            }

            if (entry is SubmenuEntry submenu)
            {
                foreach (string id in Ids(submenu.Items))
                {
                    yield return id;
                }
            }
        }
    }

    /// <summary>
    /// Adds the template entries never offered before. Each goes into the user's submenu with
    /// the same id as its template parent, right after its nearest earlier sibling the user
    /// still has, or before the nearest later one; without such a submenu, at the end of the root
    /// list. That is why a template entry carries its own match rule even inside a submenu that
    /// filters for it: on its own at the root it must still show only where it works.
    /// </summary>
    private static void AddNew(List<MenuEntry> templateLevel, SubmenuEntry? templateParent, MenuConfig config,
        HashSet<string> offered, HashSet<string> present, List<string> added)
    {
        for (int i = 0; i < templateLevel.Count; i++)
        {
            MenuEntry entry = templateLevel[i];
            bool isNew = entry.Id is { } id && !offered.Contains(id) && !present.Contains(id);
            if (isNew)
            {
                // A new submenu may hold entries the user already has, or has deleted: those
                // stay where the user left them, or gone.
                MenuEntry copy = Editing.MenuTree.Clone(entry);
                if (copy is SubmenuEntry newSubmenu && !Prune(newSubmenu, offered, present))
                {
                    continue;
                }

                Insert(config, templateParent, templateLevel, i, copy);
                foreach (string newId in Ids([copy]))
                {
                    present.Add(newId);
                    added.Add(newId);
                }

                // Its children came with it.
                continue;
            }

            if (entry is SubmenuEntry submenu)
            {
                AddNew(submenu.Items, submenu, config, offered, present, added);
            }
        }
    }

    /// <returns>False when nothing is left in the submenu but separators.</returns>
    private static bool Prune(SubmenuEntry submenu, HashSet<string> offered, HashSet<string> present)
    {
        submenu.Items.RemoveAll(child =>
            child is null
            || (child.Id is { } id && (offered.Contains(id) || present.Contains(id)))
            || (child is SubmenuEntry inner && !Prune(inner, offered, present)));
        return submenu.Items.Any(child => child is not SeparatorEntry);
    }

    private static void Insert(MenuConfig config, SubmenuEntry? templateParent, List<MenuEntry> templateLevel, int index, MenuEntry copy)
    {
        List<MenuEntry> target = config.Items;
        if (templateParent?.Id is { } parentId && Find(config.Items, parentId) is SubmenuEntry userParent)
        {
            target = userParent.Items;
        }
        else if (templateParent is not null)
        {
            config.Items.Add(copy);
            return;
        }

        for (int i = index - 1; i >= 0; i--)
        {
            if (templateLevel[i].Id is { } previousId)
            {
                int position = target.FindIndex(e => string.Equals(e?.Id, previousId, StringComparison.OrdinalIgnoreCase));
                if (position >= 0)
                {
                    target.Insert(position + 1, copy);
                    return;
                }
            }
        }

        // A new first entry goes before the nearest later sibling the user still has.
        for (int i = index + 1; i < templateLevel.Count; i++)
        {
            if (templateLevel[i].Id is { } nextId)
            {
                int position = target.FindIndex(e => string.Equals(e?.Id, nextId, StringComparison.OrdinalIgnoreCase));
                if (position >= 0)
                {
                    target.Insert(position, copy);
                    return;
                }
            }
        }

        target.Add(copy);
    }

    private static MenuEntry? Find(List<MenuEntry> entries, string id)
    {
        foreach (MenuEntry? entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            if (string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }

            if (entry is SubmenuEntry submenu && Find(submenu.Items, id) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
