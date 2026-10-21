using System.Globalization;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>
/// Sizes as people write them: "25 MB", "1.5 GB", "700m". Binary multiples, as 7-Zip and
/// Explorer count them; a bare number means megabytes. A comma works as the decimal point too.
/// </summary>
public static class SizeText
{
    private const long Kilobyte = 1024;

    /// <summary>The smallest part split accepts.</summary>
    public const long Minimum = Kilobyte;

    public static bool TryParse(string? text, out long bytes)
    {
        bytes = 0;
        string value = (text ?? "").Trim().Replace(',', '.');
        int unitStart = value.Length;
        while (unitStart > 0 && char.IsAsciiLetter(value[unitStart - 1]))
        {
            unitStart--;
        }

        long multiplier = value[unitStart..].ToUpperInvariant() switch
        {
            "B" => 1,
            "K" or "KB" or "KIB" => Kilobyte,
            "" or "M" or "MB" or "MIB" => Kilobyte * Kilobyte,
            "G" or "GB" or "GIB" => Kilobyte * Kilobyte * Kilobyte,
            _ => 0,
        };

        // Compared before multiplying: a long enough number would overflow even a decimal.
        if (multiplier == 0
            || !decimal.TryParse(value[..unitStart].Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            || number <= 0
            || number > long.MaxValue / multiplier)
        {
            return false;
        }

        bytes = (long)decimal.Round(number * multiplier);
        return bytes >= Minimum;
    }

    /// <summary>"1.5 GB", "25 MB", "700 KB", "512 bytes": the largest unit that gives at least 1.</summary>
    public static string Format(long bytes)
    {
        (long size, string unit) = bytes switch
        {
            >= Kilobyte * Kilobyte * Kilobyte => (Kilobyte * Kilobyte * Kilobyte, "GB"),
            >= Kilobyte * Kilobyte => (Kilobyte * Kilobyte, "MB"),
            >= Kilobyte => (Kilobyte, "KB"),
            _ => (1, "bytes"),
        };

        return string.Create(CultureInfo.InvariantCulture, $"{(decimal)bytes / size:0.##} {unit}");
    }
}
