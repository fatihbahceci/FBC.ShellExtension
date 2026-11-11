using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using Microsoft.Win32;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// Makes an entry's icon (ADR-0041): a <see cref="MenuIcon.StoredSize"/> PNG, from the program a
/// command starts, or from a file the user picks. Only on request; nothing here runs by itself.
/// </summary>
internal static unsafe partial class IconPicker
{
    /// <summary>The icon of the program <paramref name="command"/> starts, found as the runner finds it; null and why when it cannot be had.</summary>
    public static byte[]? FromProgram(CommandDefinition command, out string? problem)
    {
        if (VariableExpander.UsesSelection(command.FileName))
        {
            problem = Strings.EditorIconProgramUsesSelection();
            return null;
        }

        string? path = ProgramLocator.Find(command.FileName, Environment.GetEnvironmentVariable, SearchFolders(), File.Exists)
            ?? (command.UseShellExecute ? AppPath(command.FileName) : null);
        if (path is null)
        {
            problem = Strings.EditorIconProgramNotFound(command.FileName);
            return null;
        }

        return FromShellItem(path, out problem);
    }

    /// <summary>
    /// The icon in a file the user chose: a picture (.png, .ico) as it is; a program or library
    /// (.exe, .dll) by the icon picked in Windows' own dialog; anything else, a shortcut included,
    /// as Explorer shows it.
    /// </summary>
    public static byte[]? FromFile(string path, IWin32Window owner, out string? problem)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        try
        {
            switch (extension)
            {
                case ".png":
                    using (var picture = new Bitmap(path))
                    {
                        problem = null;
                        return ToPng(picture);
                    }

                case ".ico":
                    using (var icon = new Icon(path, MenuIcon.StoredSize, MenuIcon.StoredSize))
                    using (Bitmap picture = icon.ToBitmap())
                    {
                        problem = null;
                        return ToPng(picture);
                    }

                case ".exe" or ".dll":
                    return FromResource(path, owner, out problem);

                default:
                    return FromShellItem(path, out problem);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or ExternalException or UnauthorizedAccessException)
        {
            problem = ex.Message;
            return null;
        }
    }

    /// <summary>The picture of a stored icon, for showing it; null for one that does not read.</summary>
    public static Bitmap? ToBitmap(byte[]? png)
    {
        if (png is null)
        {
            return null;
        }

        try
        {
            // Image.FromStream needs its stream for the image's life; the copy does not.
            using var stream = new MemoryStream(png);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The picture scaled to the stored size and kept as PNG, alpha and all.</summary>
    private static byte[] ToPng(Image source)
    {
        int size = MenuIcon.StoredSize;
        using var scaled = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.Clear(Color.Transparent);

            // A picture that is not square keeps its proportions, centred.
            float scale = Math.Min((float)size / source.Width, (float)size / source.Height);
            float width = source.Width * scale, height = source.Height * scale;
            graphics.DrawImage(source, (size - width) / 2, (size - height) / 2, width, height);
        }

        using var stream = new MemoryStream();
        scaled.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    /// <summary>The folders a bare program name is looked for in, in the runner's order: its tools folder, its own folder, System32, Windows, then PATH.</summary>
    private static List<string> SearchFolders()
    {
        string tools = FfmpegInfo.ToolsFolder(AppContext.BaseDirectory);
        string? machine = null, user = null;
        try
        {
            using RegistryKey? machineKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            using RegistryKey? userKey = Registry.CurrentUser.OpenSubKey("Environment");
            machine = machineKey?.GetValue("Path") as string;
            user = userKey?.GetValue("Path") as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // The editor's own PATH is still there.
        }

        return
        [
            tools,
            AppContext.BaseDirectory,
            Environment.SystemDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            .. SearchPath.Split(SearchPath.Merge(machine, user, Environment.GetEnvironmentVariable("PATH"), null))
                .Select(Environment.ExpandEnvironmentVariables),
        ];
    }

    /// <summary>A program Windows knows by name (App Paths, chrome.exe for one), which only ShellExecute finds.</summary>
    private static string? AppPath(string fileName)
    {
        string name = fileName.Trim().Trim('"');
        if (name.IndexOfAny(['\\', '/']) >= 0)
        {
            return null;
        }

        name = Path.HasExtension(name) ? name : name + ".exe";
        foreach (RegistryKey root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using RegistryKey? key = root.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{name}");
                if (key?.GetValue(null) is string path && File.Exists(path = Environment.ExpandEnvironmentVariables(path.Trim('"'))))
                {
                    return path;
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // Not readable: not found there.
            }
        }

        return null;
    }

    /// <summary>The icon picked in Windows' "Change Icon" dialog, from the file's own icons.</summary>
    private static byte[]? FromResource(string path, IWin32Window owner, out string? problem)
    {
        problem = null;
        char* buffer = stackalloc char[260];
        path.AsSpan(0, Math.Min(path.Length, 259)).CopyTo(new Span<char>(buffer, 259));
        buffer[Math.Min(path.Length, 259)] = '\0';
        int index = 0;
        if (PickIconDlg(owner.Handle, buffer, 260, &index) != 1)
        {
            // Cancelled: nothing changes, and nothing is said.
            return null;
        }

        string chosen = Environment.ExpandEnvironmentVariables(new string(buffer));
        nint handle = 0;
        if (SHDefExtractIconW(chosen, index, 0, &handle, null, (uint)MenuIcon.StoredSize) != 0 || handle == 0)
        {
            problem = Strings.EditorIconNoneInFile(chosen);
            return null;
        }

        try
        {
            using var icon = Icon.FromHandle(handle);
            using Bitmap picture = icon.ToBitmap();
            return ToPng(picture);
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    /// <summary>
    /// The icon as Explorer shows the file, at the size it shows large icons (32 pixels at 100%);
    /// for a program its own, for a shortcut the one it names. The shell hands over an icon, and
    /// <see cref="Icon.ToBitmap"/> keeps its transparency; no pixels are copied here.
    /// </summary>
    private static byte[]? FromShellItem(string path, out string? problem)
    {
        problem = null;
        SHFILEINFOW info = default;
        if (SHGetFileInfoW(path, 0, &info, (uint)sizeof(SHFILEINFOW), SHGFI_ICON | SHGFI_LARGEICON) == 0 || info.hIcon == 0)
        {
            problem = Strings.EditorIconNoneInFile(path);
            return null;
        }

        try
        {
            using var icon = Icon.FromHandle(info.hIcon);
            using Bitmap picture = icon.ToBitmap();
            return ToPng(picture);
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException)
        {
            problem = ex.Message;
            return null;
        }
        finally
        {
            DestroyIcon(info.hIcon);
        }
    }

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;

    /// <summary>shellapi.h; 696 bytes on x64.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFOW
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        public fixed char szDisplayName[260];
        public fixed char szTypeName[80];
    }

    [LibraryImport("shell32.dll", EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nuint SHGetFileInfoW(string path, uint fileAttributes, SHFILEINFOW* info, uint size, uint flags);

    /// <summary>Returns 1 when an icon was chosen.</summary>
    [LibraryImport("shell32.dll", EntryPoint = "PickIconDlg")]
    private static partial int PickIconDlg(nint hwnd, char* iconPath, uint length, int* index);

    [LibraryImport("shell32.dll", EntryPoint = "SHDefExtractIconW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHDefExtractIconW(string iconFile, int index, uint flags, nint* large, nint* small, uint size);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(nint icon);

}
