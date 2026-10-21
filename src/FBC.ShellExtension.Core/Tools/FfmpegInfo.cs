using System.Globalization;
using System.Text.RegularExpressions;

namespace FBC.ShellExtension.Core.Tools;

/// <param name="Path">The ffmpeg.exe that would run; null when none was found.</param>
/// <param name="Version">From <c>ffmpeg -version</c>, such as <c>9.0.2-full_build-www.gyan.dev</c>.</param>
/// <param name="MissingEncoders">Encoders the shipped template uses that this build lacks.</param>
public sealed record FfmpegStatus(string? Path, string? Version, IReadOnlyList<string> MissingEncoders)
{
    public static FfmpegStatus NotFound { get; } = new(null, null, []);

    public bool Found => Path is not null;

    public bool Usable => Found && MissingEncoders.Count == 0;
}

/// <summary>
/// What FBCTools knows about ffmpeg, the program its template's conversions run (ADR-0011):
/// how to read its answers, and where the two install routes get it from.
/// </summary>
public static partial class FfmpegInfo
{
    public const string ExeName = "ffmpeg.exe";

    /// <summary>The encoders the template's commands name (MP3, MP4 with H.264 and AAC).</summary>
    public static IReadOnlyList<string> RequiredEncoders { get; } = ["libmp3lame", "libx264", "aac"];

    /// <summary>Where a downloaded ffmpeg goes: <c>tools\ffmpeg</c> in the program folder.</summary>
    public static string ToolsFolder(string programFolder) => System.IO.Path.Combine(programFolder, "tools", "ffmpeg");

    /// <summary>
    /// The tools-folder file a bare program name stands for (<c>ffmpeg</c>, <c>ffmpeg.exe</c>);
    /// null for a name with a folder in it, or one the tools folder does not have. Windows
    /// searches System32 before PATH, so a downloaded ffmpeg is started by its full path.
    /// </summary>
    public static string? FromToolsFolder(string fileName, string toolsFolder, Func<string, bool> exists)
    {
        if (fileName.Length == 0 || fileName.IndexOfAny(['\\', '/', ':']) >= 0)
        {
            return null;
        }

        string name = fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".exe";
        string candidate = System.IO.Path.Combine(toolsFolder, name);
        return exists(candidate) ? candidate : null;
    }

    // ---- reading ffmpeg's answers -------------------------------------------------------------

    /// <summary>The version from the first line of <c>ffmpeg -version</c>: "ffmpeg version X Copyright ...".</summary>
    public static string? ParseVersion(string output)
    {
        Match match = VersionLine().Match(output);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// The encoder names from <c>ffmpeg -hide_banner -encoders</c>: the table after the line of
    /// dashes, each row a flags column and then the name. The exit code says nothing about them.
    /// </summary>
    public static IReadOnlySet<string> ParseEncoders(string output)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool inTable = false;
        foreach (string line in output.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!inTable)
            {
                inTable = trimmed.StartsWith("------", StringComparison.Ordinal);
                continue;
            }

            string[] columns = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length >= 2)
            {
                names.Add(columns[1]);
            }
        }

        return names;
    }

    public static IReadOnlyList<string> Missing(IReadOnlySet<string> encoders) =>
        [.. RequiredEncoders.Where(name => !encoders.Contains(name))];

    // ---- winget ------------------------------------------------------------------------------

    /// <summary>The full gyan.dev build: GPL-3.0, has every encoder the template uses, kept current.</summary>
    public const string WingetPackageId = "Gyan.FFmpeg";

    /// <summary>A user-scope, non-interactive install; the user has agreed in the editor first.</summary>
    public static IReadOnlyList<string> WingetInstallArguments { get; } =
    [
        "install", "--id", WingetPackageId, "--exact", "--source", "winget", "--scope", "user",
        "--accept-source-agreements", "--accept-package-agreements", "--disable-interactivity", "--silent",
    ];

    /// <summary>0, or 0x8A15002B: already installed and no newer version.</summary>
    public static bool IsWingetSuccess(int exitCode) => exitCode is 0 or unchecked((int)0x8A15002B);

    // ---- direct download (gyan.dev release essentials) ---------------------------------------

    /// <summary>A one-line file with the current release version, such as <c>9.0.2</c>.</summary>
    public const string ReleaseVersionUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.ver";

    /// <summary>The GitHub mirror gyan publishes byte for byte; winget downloads from it too.</summary>
    public static string MirrorZipUrl(string version) =>
        $"https://github.com/GyanD/codexffmpeg/releases/download/{version}/ffmpeg-{version}-essentials_build.zip";

    public static string GyanZipUrl(string version) =>
        $"https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-{version}-essentials_build.zip";

    /// <summary>The checksum of exactly that version, from another host than the mirror's zip.</summary>
    public static string GyanSha256Url(string version) =>
        $"https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-{version}-essentials_build.zip.sha256";

    public static bool IsReleaseVersion(string text) => ReleaseVersion().IsMatch(text);

    /// <summary>The 64 hex digits of a .sha256 file, upper case; null when the text is not one.</summary>
    public static string? ParseSha256(string text)
    {
        string first = text.Trim().Split((char[])[' ', '\t', '\r', '\n'], 2)[0];
        return Sha256().IsMatch(first) ? first.ToUpper(CultureInfo.InvariantCulture) : null;
    }

    /// <summary>The archive entry of ffmpeg.exe, whatever the top folder is called this release.</summary>
    public static string? FindExeEntry(IEnumerable<string> entryNames) =>
        entryNames.FirstOrDefault(name => name.Replace('\\', '/').EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"^ffmpeg version (\S+)", RegexOptions.Multiline)]
    private static partial Regex VersionLine();

    [GeneratedRegex(@"^\d+\.\d+(\.\d+)?$")]
    private static partial Regex ReleaseVersion();

    [GeneratedRegex(@"^[0-9A-Fa-f]{64}$")]
    private static partial Regex Sha256();
}
