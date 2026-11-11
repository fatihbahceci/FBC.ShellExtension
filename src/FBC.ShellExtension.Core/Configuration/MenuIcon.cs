using System.Buffers.Binary;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>
/// An entry's icon (ADR-0041): a PNG, base64 in the config, shown before the label in the classic
/// menu. Checked here without decoding the picture: Core reads no image formats, and the shell
/// extension decodes it with Windows' own codec only after these checks. Text that is not base64
/// does not get this far: the file does not read (the error names the entry).
/// </summary>
public static class MenuIcon
{
    /// <summary>The size the editor stores; the menu scales it to its small-icon size (16 at 100%, 32 at 200%).</summary>
    public const int StoredSize = 32;

    /// <summary>A 32×32 icon is a few KB; this leaves room for a larger one without letting a config carry pictures.</summary>
    public const int MaxBytes = 16 * 1024;

    /// <summary>The largest width or height read; Windows' decoder is not asked to open more.</summary>
    public const int MaxDimension = 256;

    private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Why <paramref name="png"/> is not an icon this version can show; null when it is one.</summary>
    public static string? Problem(byte[] png)
    {
        if (png.Length > MaxBytes)
        {
            return Strings.ValidationIconTooLarge((png.Length + 1023) / 1024, MaxBytes / 1024);
        }

        if (!TryReadSize(png, out int width, out int height))
        {
            return Strings.ValidationIconNotPng();
        }

        return width > MaxDimension || height > MaxDimension
            ? Strings.ValidationIconDimensions(width, height, MaxDimension)
            : null;
    }

    /// <summary>The width and height a PNG declares in its header; false when the bytes do not start like a PNG.</summary>
    public static bool TryReadSize(ReadOnlySpan<byte> png, out int width, out int height)
    {
        width = height = 0;

        // The signature, then the IHDR chunk: its length (13), its type, the width and the height.
        if (png.Length < 33 || !png[..8].SequenceEqual(Signature)
            || BinaryPrimitives.ReadUInt32BigEndian(png[8..]) != 13 || !png[12..16].SequenceEqual("IHDR"u8))
        {
            return false;
        }

        uint w = BinaryPrimitives.ReadUInt32BigEndian(png[16..]);
        uint h = BinaryPrimitives.ReadUInt32BigEndian(png[20..]);
        if (w == 0 || h == 0 || w > int.MaxValue || h > int.MaxValue)
        {
            return false;
        }

        width = (int)w;
        height = (int)h;
        return true;
    }
}
