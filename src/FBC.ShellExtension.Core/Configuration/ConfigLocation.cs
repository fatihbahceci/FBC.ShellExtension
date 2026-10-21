namespace FBC.ShellExtension.Core.Configuration;

/// <summary>The config is per user: <c>%APPDATA%\FBCTools\config.json</c> (ADR-0005).</summary>
public static class ConfigLocation
{
    public const string FileName = "config.json";

    public static string FolderPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductInfo.Name);

    public static string DefaultPath => Path.Combine(FolderPath, FileName);

    /// <summary>
    /// A free name next to the config for a copy of it, such as <c>config.20261020-185842.bak.json</c>;
    /// the same form the dev-run script uses.
    /// </summary>
    public static string BackupPath(string configPath, DateTime time)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
        string name = Path.GetFileNameWithoutExtension(configPath);
        string stamp = time.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        string candidate = Path.Combine(folder, $"{name}.{stamp}.bak.json");
        for (int i = 2; File.Exists(candidate); i++)
        {
            candidate = Path.Combine(folder, $"{name}.{stamp}-{i}.bak.json");
        }

        return candidate;
    }
}
