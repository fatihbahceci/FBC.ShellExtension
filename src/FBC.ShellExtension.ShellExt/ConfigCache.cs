using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.ShellExt;

internal enum ConfigStatus
{
    Missing,
    Invalid,
    Valid,
}

internal sealed record ConfigSnapshot(ConfigStatus Status, MenuConfig? Config)
{
    public static ConfigSnapshot Missing { get; } = new(ConfigStatus.Missing, null);

    public static ConfigSnapshot Invalid { get; } = new(ConfigStatus.Invalid, null);
}

/// <summary>
/// Reads the config once and again only when its time stamp or size changes, so an edit is
/// picked up on the next right-click without restarting Explorer. Shared by every Explorer
/// window, each of which calls in on its own thread.
/// </summary>
internal static class ConfigCache
{
    private static readonly Lock s_lock = new();
    private static Entry? s_entry;

    private sealed record Entry(string Path, DateTime LastWriteUtc, long Length, ConfigSnapshot Snapshot);

    public static ConfigSnapshot Get()
    {
        string path = ConfigLocation.DefaultPath;
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            return ConfigSnapshot.Missing;
        }

        DateTime lastWriteUtc = info.LastWriteTimeUtc;
        long length = info.Length;
        lock (s_lock)
        {
            if (s_entry is { } entry && entry.Path == path && entry.LastWriteUtc == lastWriteUtc && entry.Length == length)
            {
                return entry.Snapshot;
            }
        }

        ConfigSnapshot snapshot;
        try
        {
            MenuConfig config = ConfigSerializer.Load(path);
            IReadOnlyList<ConfigIssue> issues = ConfigValidator.Validate(config);
            foreach (ConfigIssue issue in issues)
            {
                Diagnostics.Trace($"Config: {issue.Path}: {issue.Message}");
            }

            snapshot = issues.Count == 0 ? new ConfigSnapshot(ConfigStatus.Valid, config) : ConfigSnapshot.Invalid;
        }
        catch (ConfigLoadException ex)
        {
            Diagnostics.Trace($"Config: {ex.Message}");
            snapshot = ConfigSnapshot.Invalid;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Most likely the editor is writing the file right now. Not cached: the next
            // right-click tries again even if the time stamp has not moved.
            Diagnostics.Trace($"Config: {ex.Message}");
            return ConfigSnapshot.Invalid;
        }

        lock (s_lock)
        {
            s_entry = new Entry(path, lastWriteUtc, length, snapshot);
        }

        return snapshot;
    }
}
