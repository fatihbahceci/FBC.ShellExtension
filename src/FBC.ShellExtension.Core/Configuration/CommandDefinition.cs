using System.Text.Json.Serialization;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>
/// What a menu item runs. The process properties mirror <c>ProcessStartInfo</c>; FileName,
/// Arguments and WorkingDirectory may contain placeholders and <c>%ENV%</c> variables.
/// </summary>
public sealed class CommandDefinition
{
    public const string RunAsVerb = "runas";

    public string FileName { get; set; } = "";

    public string? Arguments { get; set; }

    /// <summary>When empty the process starts in <c>{dir}</c>.</summary>
    public string? WorkingDirectory { get; set; }

    public CommandWindowStyle WindowStyle { get; set; } = CommandWindowStyle.Normal;

    public bool CreateNoWindow { get; set; }

    public bool UseShellExecute { get; set; }

    /// <summary><c>runas</c> elevates the process; any other verb requires <see cref="UseShellExecute"/>.</summary>
    public string? Verb { get; set; }

    public RunMode RunMode { get; set; } = RunMode.Track;

    public MultiSelectMode MultiSelect { get; set; } = MultiSelectMode.PerFileSequential;

    /// <summary>Used by <see cref="MultiSelectMode.PerFileParallel"/>; null leaves the choice to the runner.</summary>
    public int? MaxParallelism { get; set; }

    /// <summary>Track mode: close the runner window shortly after every process succeeded.</summary>
    public bool CloseOnSuccess { get; set; } = true;

    /// <summary>
    /// Track mode: show what the processes write to stdout and stderr in the runner window as it
    /// arrives. Needs <see cref="UseShellExecute"/> off, since only then can output be captured.
    /// </summary>
    public bool ShowOutput { get; set; }

    /// <summary>
    /// How the process encodes what it writes to stderr and stdout. Console programs mostly
    /// use the OEM code page; some, such as ffmpeg, write UTF-8 (G-0104).
    /// </summary>
    public CommandOutputEncoding OutputEncoding { get; set; } = CommandOutputEncoding.Oem;

    [JsonIgnore]
    public bool RunsElevated => string.Equals(Verb, RunAsVerb, StringComparison.OrdinalIgnoreCase);
}

[JsonConverter(typeof(CamelCaseEnumConverter<CommandWindowStyle>))]
public enum CommandWindowStyle
{
    Normal,
    Hidden,
    Minimized,
    Maximized,
}

[JsonConverter(typeof(CamelCaseEnumConverter<CommandOutputEncoding>))]
public enum CommandOutputEncoding
{
    /// <summary>The console code page, 857 on a Turkish system; what cmd and most console programs write.</summary>
    Oem,

    Utf8,

    /// <summary>The Windows code page, 1254 on a Turkish system.</summary>
    Ansi,
}

[JsonConverter(typeof(CamelCaseEnumConverter<RunMode>))]
public enum RunMode
{
    /// <summary>Start the process and return; only a failure to start is reported.</summary>
    Launch,

    /// <summary>Wait for the process, collect exit code and stderr, show a summary.</summary>
    Track,
}

[JsonConverter(typeof(CamelCaseEnumConverter<MultiSelectMode>))]
public enum MultiSelectMode
{
    /// <summary>One process per item, one after another.</summary>
    PerFileSequential,

    /// <summary>One process per item, up to <see cref="CommandDefinition.MaxParallelism"/> at a time.</summary>
    PerFileParallel,

    /// <summary>One process for the whole selection; use <c>{allPaths}</c> to pass it.</summary>
    AllAtOnce,
}
