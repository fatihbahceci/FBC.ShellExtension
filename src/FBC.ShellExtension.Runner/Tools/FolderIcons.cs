using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.Execution;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// A folder's own icon, the documented way (ADR-0026): Windows' icon picker, and
/// SHGetSetFolderCustomSettings writing the folder's desktop.ini. What that function does to
/// the attributes is not documented, so the folder is marked customized here as well, and on a
/// restore the icon lines go and an empty desktop.ini with them.
/// </summary>
internal static unsafe class FolderIcons
{
    private const int MaxPath = 260;
    private const string DesktopIni = "desktop.ini";

    /// <summary>Where the icon picker opens for a folder with no icon of its own.</summary>
    public const string DefaultIconFile = @"%SystemRoot%\System32\imageres.dll";

    private static readonly Guid s_knownFolderManager = new("4DF0C730-DF9D-4AE3-9153-AA6B82E9795A");
    private static readonly Guid s_iKnownFolderManager = new("8BE2D872-86AA-4D47-B776-32CCA40C7018");

    // IKnownFolderManager: IUnknown (0-2) … FindFolderFromPath (10).
    private const int FindFolderFromPathSlot = 10;
    private const int FffpExactMatch = 0;

    /// <summary>The folder's icon file and index; null when it has none of its own.</summary>
    public static (string File, int Index)? Current(string folder)
    {
        char* buffer = stackalloc char[MaxPath];
        buffer[0] = '\0';
        FolderCustomSettings settings = default;
        settings.Size = (uint)sizeof(FolderCustomSettings);
        settings.Mask = FCSM_ICONFILE;
        settings.IconFile = buffer;
        settings.IconFileLength = MaxPath;
        fixed (char* path = folder)
        {
            if (SHGetSetFolderCustomSettings(&settings, path, FCS_READ) < 0 || buffer[0] == '\0')
            {
                return null;
            }
        }

        return (new string(buffer), settings.IconIndex);
    }

    /// <summary>Windows' icon picker, opened on <paramref name="file"/>.</summary>
    /// <returns>The chosen file, as the picker gives it (it may hold %SystemRoot%), and index; null on cancel.</returns>
    /// <exception cref="ToolFailedException">The picker is not there.</exception>
    public static (string File, int Index)? Pick(string file, int index)
    {
        // The picker writes into the buffer, and reads the index even when told it is optional.
        char* buffer = stackalloc char[MaxPath];
        int length = Math.Min(file.Length, MaxPath - 1);
        file.AsSpan(0, length).CopyTo(new Span<char>(buffer, MaxPath));
        buffer[length] = '\0';
        int chosen = index;
        int result;
        try
        {
            result = PickIconDlg(0, buffer, MaxPath, &chosen);
        }
        catch (EntryPointNotFoundException ex)
        {
            throw new ToolFailedException(Strings.ToolPickerFailed(ex.Message));
        }

        return result == 1 ? (new string(buffer), chosen) : null;
    }

    /// <summary>One of Windows' own folders (Documents, Pictures …): its desktop.ini carries Windows' icon and name.</summary>
    public static bool IsWindowsFolder(string folder)
    {
        using var apartment = new ShellCom.Apartment();
        Guid clsid = s_knownFolderManager;
        Guid iid = s_iKnownFolderManager;
        nint manager = 0;
        if (CoCreateInstance(&clsid, 0, CLSCTX_INPROC_SERVER, &iid, &manager) < 0 || manager == 0)
        {
            return false;
        }

        try
        {
            nint known = 0;
            int hresult;
            fixed (char* path = folder)
            {
                hresult = ((delegate* unmanaged<nint, char*, int, nint*, int>)(*(nint**)manager)[FindFolderFromPathSlot])(manager, path, FffpExactMatch, &known);
            }

            ShellCom.Release(known);
            return hresult >= 0 && known != 0;
        }
        finally
        {
            ShellCom.Release(manager);
        }
    }

    /// <exception cref="ToolFailedException">Windows refused.</exception>
    public static void Set(string folder, string file, int index)
    {
        FolderCustomSettings settings = default;
        settings.Size = (uint)sizeof(FolderCustomSettings);
        settings.Mask = FCSM_ICONFILE;
        settings.IconIndex = index;
        int hresult;
        fixed (char* path = folder, icon = file)
        {
            settings.IconFile = icon;
            hresult = SHGetSetFolderCustomSettings(&settings, path, FCS_FORCEWRITE);
            if (hresult >= 0)
            {
                // Explorer reads desktop.ini only in a folder marked read-only or system.
                PathMakeSystemFolderW(path);
            }
        }

        if (hresult < 0)
        {
            throw new ToolFailedException(Strings.ToolIconFailed(Path.GetFileName(folder.TrimEnd('\\')), ShellCom.Describe(hresult)));
        }

        Notify(folder);
    }

    /// <exception cref="ToolFailedException">Windows refused.</exception>
    public static void Restore(string folder)
    {
        string name = Path.GetFileName(folder.TrimEnd('\\'));
        FolderCustomSettings settings = default;
        settings.Size = (uint)sizeof(FolderCustomSettings);
        settings.Mask = FCSM_ICONFILE;
        int hresult;
        fixed (char* path = folder)
        {
            hresult = SHGetSetFolderCustomSettings(&settings, path, FCS_FORCEWRITE);
        }

        if (hresult < 0)
        {
            throw new ToolFailedException(Strings.ToolIconFailed(name, ShellCom.Describe(hresult)));
        }

        string ini = Path.Combine(folder, DesktopIni);
        try
        {
            if (File.Exists(ini))
            {
                fixed (char* section = ".ShellClassInfo", file = ini)
                {
                    foreach (string key in (string[])["IconResource", "IconFile", "IconIndex"])
                    {
                        fixed (char* keyText = key)
                        {
                            WritePrivateProfileStringW(section, keyText, null, file);
                        }
                    }

                    // Writes what Windows keeps cached of the file; returns 0 by design.
                    WritePrivateProfileStringW(null, null, null, file);
                }
            }

            // A desktop.ini with something else in it (a name, an info tip) keeps the folder customized.
            if (!File.Exists(ini) || !FileTools.DesktopIniHasOtherSettings(TextDecoder.Decode(File.ReadAllBytes(ini), CodePages.Ansi) ?? "x=x"))
            {
                if (File.Exists(ini))
                {
                    File.SetAttributes(ini, FileAttributes.Normal);
                    File.Delete(ini);
                }

                fixed (char* path = folder)
                {
                    PathUnmakeSystemFolderW(path);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ToolFailedException(Strings.ToolIconFailed(name, ex.Message));
        }

        Notify(folder);
    }

    /// <summary>Makes Explorer show the change at once: the folder where it is listed, and what is in it.</summary>
    private static void Notify(string folder)
    {
        fixed (char* path = folder)
        {
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH, path, 0);
            SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSH, path, 0);
        }
    }
}
