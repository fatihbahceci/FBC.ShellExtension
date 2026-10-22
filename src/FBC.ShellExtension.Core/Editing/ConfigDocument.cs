using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Editing;

/// <summary>What the file looked like when it was last read or written.</summary>
public sealed record FileStamp(bool Exists, DateTime LastWriteUtc, long Length)
{
    public static FileStamp Of(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? new FileStamp(true, info.LastWriteTimeUtc, info.Length) : new FileStamp(false, default, 0);
    }
}

/// <summary>
/// The config file the editor works on: the document in memory, whether it differs from what
/// was last saved, whether the file changed underneath, and the backup rule (ADR-0007).
/// </summary>
public sealed class ConfigDocument
{
    private string _savedJson;
    private FileStamp _stamp;
    private bool _replacesUnreadableFile;
    private bool _backedUp;

    private ConfigDocument(string path, MenuConfig config, bool hasComments, bool replacesUnreadableFile)
    {
        Path = path;
        Config = config;
        HasComments = hasComments;
        _replacesUnreadableFile = replacesUnreadableFile;
        _savedJson = ConfigSerializer.Serialize(config);
        _stamp = FileStamp.Of(path);
    }

    public string Path { get; }

    /// <summary>Replaced as a whole by undo and redo.</summary>
    public MenuConfig Config { get; set; }

    /// <summary>The file on disk has comments, which saving drops.</summary>
    public bool HasComments { get; private set; }

    public bool IsDirty => ConfigSerializer.Serialize(Config) != _savedJson;

    /// <summary>The next save first copies the file aside: it has comments, or it could not be read at all.</summary>
    public bool BacksUpOnSave => (HasComments || _replacesUnreadableFile) && !_backedUp && File.Exists(Path);

    /// <exception cref="ConfigLoadException">The file is not a valid configuration document.</exception>
    public static ConfigDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            return new ConfigDocument(path, new MenuConfig(), hasComments: false, replacesUnreadableFile: false);
        }

        string text = File.ReadAllText(path);
        MenuConfig config = ConfigSerializer.Deserialize(text);
        return new ConfigDocument(path, config, ConfigSerializer.ContainsComments(text), replacesUnreadableFile: false);
    }

    /// <summary>An empty document for a file that could not be read; that file is backed up before it is overwritten.</summary>
    public static ConfigDocument StartOver(string path) =>
        new(path, new MenuConfig(), hasComments: false, replacesUnreadableFile: File.Exists(path));

    /// <summary>The file was written, replaced or deleted by someone else since it was read or saved here.</summary>
    public bool ChangedOnDisk() => FileStamp.Of(Path) != _stamp;

    /// <summary>Stops <see cref="ChangedOnDisk"/> from reporting the current state of the file again.</summary>
    public void AcceptDiskState() => _stamp = FileStamp.Of(Path);

    /// <summary>
    /// The file changed on disk and could not be read back, and the user kept the document in
    /// memory. The next save copies that file aside before it overwrites it.
    /// </summary>
    public void KeepDespiteUnreadableFile()
    {
        _replacesUnreadableFile = true;
        _backedUp = false;
        AcceptDiskState();
    }

    /// <summary>Writes the document, copying the old file aside first when <see cref="BacksUpOnSave"/>.</summary>
    /// <returns>The backup's path, or null when none was made.</returns>
    /// <exception cref="ConfigNewerException">
    /// A newer FBCTools wrote the file since: saving this document over it would drop what that
    /// version added, so nothing is written (ADR-0038).
    /// </exception>
    public string? Save(DateTime now)
    {
        if (File.Exists(Path) && ConfigSerializer.DeclaredVersion(File.ReadAllText(Path)) is int onDisk
            && onDisk > MenuConfig.CurrentVersion && onDisk > Config.Version)
        {
            throw new ConfigNewerException(Strings.ConfigNewerOnDisk(onDisk, MenuConfig.CurrentVersion), onDisk, null);
        }

        string? backup = null;
        if (BacksUpOnSave)
        {
            backup = ConfigLocation.BackupPath(Path, now);
            File.Copy(Path, backup);
            _backedUp = true;
        }

        ConfigSerializer.Save(Config, Path);
        _savedJson = ConfigSerializer.Serialize(Config);
        _stamp = FileStamp.Of(Path);
        HasComments = false;
        _replacesUnreadableFile = false;
        return backup;
    }
}
