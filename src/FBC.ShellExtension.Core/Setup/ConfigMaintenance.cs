using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Templates;

namespace FBC.ShellExtension.Core.Setup;

/// <param name="Backup">Where the config was copied before it was overwritten; null when nothing was.</param>
/// <param name="AppliedBy">The FBCTools version that last applied a template.</param>
public sealed record MaintenanceResult(TemplateAction Action, IReadOnlyList<string> AddedIds, string? Backup, string? AppliedBy);

/// <summary>
/// The config operations of the manager (Aşama 5): bring the menu up to the template, back up,
/// restore, reset. Whatever overwrites a config the user may have changed copies it aside first
/// (ADR-0007, ADR-0009).
/// </summary>
public static class ConfigMaintenance
{
    /// <exception cref="TemplateLoadException">The template cannot be read.</exception>
    /// <exception cref="ConfigLoadException">A config that has to be merged cannot be read.</exception>
    public static MaintenanceResult UpdateFromTemplate(string configPath, string templatePath, DateTime now, Version version)
    {
        string template = File.ReadAllText(templatePath);
        string? config = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
        string statePath = TemplateState.PathFor(configPath);
        TemplateOutcome outcome = TemplateMerger.Apply(template, config, TemplateState.Load(statePath), version);

        string? backup = null;
        if (outcome.ConfigText is { } text)
        {
            if (outcome.Action == TemplateAction.Merged)
            {
                backup = CopyAside(configPath, now);
            }

            ConfigSerializer.SaveText(text, configPath);
        }

        outcome.State.Save(statePath);
        return new MaintenanceResult(outcome.Action, outcome.AddedIds, backup, outcome.State.AppliedBy);
    }

    /// <exception cref="FileNotFoundException">There is no config.</exception>
    public static void Backup(string configPath, string targetPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(null, configPath);
        }

        File.Copy(configPath, Path.GetFullPath(targetPath), overwrite: true);
    }

    /// <summary>Replaces the config with a file the user chose; it counts as the user's own from then on.</summary>
    /// <exception cref="ConfigLoadException">The file is not a configuration document.</exception>
    public static string? Restore(string configPath, string sourcePath, DateTime now)
    {
        string text = File.ReadAllText(sourcePath);
        ConfigSerializer.Deserialize(text);
        string? backup = CopyAside(configPath, now);
        ConfigSerializer.SaveText(text, configPath);

        string statePath = TemplateState.PathFor(configPath);
        if (TemplateState.Load(statePath) is { } state)
        {
            state.ConfigHash = null;
            state.Save(statePath);
        }

        return backup;
    }

    /// <summary>Puts the template back as the config; later updates treat it as unchanged again.</summary>
    /// <exception cref="TemplateLoadException">The template cannot be read.</exception>
    public static string? Reset(string configPath, string templatePath, DateTime now, Version version)
    {
        string template = File.ReadAllText(templatePath);
        TemplateState created = TemplateMerger.Apply(template, null, null, version).State;
        string? backup = CopyAside(configPath, now);
        ConfigSerializer.SaveText(template, configPath);

        string statePath = TemplateState.PathFor(configPath);
        if (TemplateState.Load(statePath) is { } previous)
        {
            created.AppliedIds = [.. previous.AppliedIds.Concat(created.AppliedIds).Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        created.Save(statePath);
        return backup;
    }

    private static string? CopyAside(string configPath, DateTime now)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        string backup = ConfigLocation.BackupPath(configPath, now);
        File.Copy(configPath, backup);
        return backup;
    }
}

/// <summary>What the manager's exit code tells the editor and scripts.</summary>
public enum ManagerExitCode
{
    Success = 0,

    /// <summary>Done, but something needs the user's attention: another copy is registered, the template is missing.</summary>
    Attention = 1,

    Usage = 2,

    Failed = 3,
}
