using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;
using FBC.ShellExtension.Editor.Tools;
using FBC.ShellExtension.WikiImport;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// What goes into a backup from this computer and what a backup puts back (ADR-0048). The
/// settings files keep their keys encrypted for one Windows user (DPAPI); a backup takes them
/// out of DPAPI, so that another computer can read them, and a restore puts them back into it
/// for the user restoring.
/// </summary>
internal static class Backups
{
    /// <summary>The tools' settings as a backup holds them: the files' text without the keys, and the keys apart.</summary>
    /// <param name="Unreadable">Keys this Windows user cannot decrypt (another user's or computer's): left out.</param>
    /// <param name="Problems">Settings files that could not be read: left out.</param>
    public sealed record Settings(string? Omdb, string? Wiki, BackupKeys Keys, IReadOnlyList<string> Unreadable, IReadOnlyList<string> Problems);

    public static Settings Collect(DpapiProtector protector)
    {
        var keys = new BackupKeys();
        var unreadable = new List<string>();
        var problems = new List<string>();
        string? omdb = null;
        if (File.Exists(OmdbSettings.DefaultPath))
        {
            try
            {
                OmdbSettings settings = OmdbSettings.Load(OmdbSettings.DefaultPath);
                if (settings.ApiKey is not null)
                {
                    if (settings.Key(protector) is { } key)
                    {
                        keys.Omdb = key;
                    }
                    else
                    {
                        unreadable.Add(Strings.BackupKeyOmdb());
                    }

                    settings.ApiKey = null;
                }

                omdb = settings.ToJson();
            }
            catch (IOException ex)
            {
                problems.Add(ex.Message);
            }
        }

        string? wiki = null;
        if (File.Exists(WikiSettings.DefaultPath))
        {
            try
            {
                WikiSettings settings = WikiSettings.Load(WikiSettings.DefaultPath);
                foreach (WikiSiteSettings site in settings.Sites.Where(site => site.ApiKey is not null))
                {
                    if (protector.Unprotect(site.ApiKey!) is { } key)
                    {
                        keys.Wiki[site.BaseUrl] = key;
                    }
                    else
                    {
                        unreadable.Add(Strings.BackupKeyWiki(site.BaseUrl));
                    }

                    site.ApiKey = null;
                }

                wiki = settings.ToJson();
            }
            catch (WikiException ex)
            {
                problems.Add(ex.Message);
            }
        }

        return new Settings(omdb, wiki, keys, unreadable, problems);
    }

    /// <summary>
    /// Puts a backup's settings back for this Windows user; the file there before is copied
    /// aside first. What the backup does not carry stays as this computer has it: a key left out
    /// (or restored without the keys), a Wiki.js site the backup's computer did not know, and the
    /// page a file went to here. A settings file here that cannot be read is not replaced: its
    /// keys would be lost with it.
    /// </summary>
    /// <param name="backupName">The backup's file name, for what its own entries say.</param>
    /// <returns>A line for each settings file put back or not.</returns>
    public static List<string> RestoreSettings(BackupContent content, string backupName, BackupKeys? keys, DpapiProtector protector, DateTime now)
    {
        var lines = new List<string>();
        if (content.OmdbSettings is { } omdbText)
        {
            lines.Add(RestoreOmdb(omdbText, backupName, keys, protector, now));
        }

        if (content.WikiSettings is { } wikiText)
        {
            lines.Add(RestoreWiki(wikiText, backupName, keys, protector, now));
        }

        return lines;
    }

    private static string RestoreOmdb(string text, string backupName, BackupKeys? keys, DpapiProtector protector, DateTime now)
    {
        OmdbSettings settings;
        try
        {
            settings = OmdbSettings.FromJson(text, OmdbSettings.FileName);
        }
        catch (IOException)
        {
            return Strings.BackupEntryUnreadable(OmdbSettings.FileName, backupName);
        }

        try
        {
            // Read first: an unreadable file here stops this one, as it would take its key along.
            OmdbSettings current = OmdbSettings.Load(OmdbSettings.DefaultPath);
            if (keys?.Omdb is { } key)
            {
                settings.SetKey(key, protector);
            }
            else
            {
                settings.ApiKey = current.ApiKey;
            }

            CopyAside(OmdbSettings.DefaultPath, now);
            settings.Save(OmdbSettings.DefaultPath);
            return keys?.Omdb is null ? Strings.BackupRestoredOmdb() : Strings.BackupRestoredOmdbKey();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Strings.BackupNotRestored(OmdbSettings.FileName, ex.Message);
        }
    }

    private static string RestoreWiki(string text, string backupName, BackupKeys? keys, DpapiProtector protector, DateTime now)
    {
        WikiSettings settings;
        try
        {
            settings = WikiSettings.FromJson(text, WikiSettings.FileName);
        }
        catch (WikiException)
        {
            return Strings.BackupEntryUnreadable(WikiSettings.FileName, backupName);
        }

        try
        {
            WikiSettings current = WikiSettings.Load(WikiSettings.DefaultPath);
            foreach (WikiSiteSettings site in settings.Sites)
            {
                site.ApiKey = current.Sites.FirstOrDefault(old => string.Equals(old.BaseUrl, site.BaseUrl, StringComparison.OrdinalIgnoreCase))?.ApiKey;
            }

            // What only this computer knows stays: its other sites with their keys, and its pages.
            settings.Sites.AddRange(current.Sites.Where(old =>
                !settings.Sites.Any(site => string.Equals(site.BaseUrl, old.BaseUrl, StringComparison.OrdinalIgnoreCase))));
            settings.Targets.AddRange(current.Targets.Where(old =>
                !settings.Targets.Any(target => string.Equals(target.File, old.File, StringComparison.OrdinalIgnoreCase))));
            foreach ((string site, string key) in keys?.Wiki ?? [])
            {
                settings.SetKey(site, key, protector);
            }

            CopyAside(WikiSettings.DefaultPath, now);
            settings.Save(WikiSettings.DefaultPath);
            return Strings.BackupRestoredWiki(keys?.Wiki.Count ?? 0);
        }
        catch (Exception ex) when (ex is WikiException or IOException or UnauthorizedAccessException)
        {
            return Strings.BackupNotRestored(WikiSettings.FileName, ex.Message);
        }
    }

    private static void CopyAside(string path, DateTime now)
    {
        if (File.Exists(path))
        {
            File.Copy(path, ConfigLocation.BackupPath(path, now));
        }
    }
}
