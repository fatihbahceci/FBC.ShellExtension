using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Moves and deletes with Explorer's own engine (IFileOperation), as the runner copies
/// (ADR-0026, ADR-0028): its progress window, its question when a name is taken, the Recycle
/// Bin. Windows reports what went wrong with a file in its own dialogs; what is left here is
/// what kept the operation from starting. Called on the window's thread, an STA.
/// </summary>
internal static unsafe partial class ShellFileOperation
{
    private const uint CLSCTX_INPROC_SERVER = 0x1;

    private const uint FOF_NOCONFIRMATION = 0x10;
    private const uint FOF_ALLOWUNDO = 0x40;
    private const uint FOF_NOCONFIRMMKDIR = 0x200;
    private const uint FOF_WANTNUKEWARNING = 0x4000;
    private const uint FOFX_ADDUNDORECORD = 0x20000000;

    // ERROR_CANCELLED as an HRESULT, and the copy engine's own "the user cancelled".
    private const int HRESULT_CANCELLED = unchecked((int)0x800704C7);
    private const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);

    private static readonly Guid s_fileOperation = new("3AD05575-8857-4850-9277-11B85BDB8E09");
    private static readonly Guid s_shellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
    private static readonly StrategyBasedComWrappers s_wrappers = new();

    /// <summary>Moves each file into its folder, all in one operation; the folders must exist.</summary>
    /// <returns>A line for each file that could not be handed over, and for an operation that failed as a whole.</returns>
    public static List<string> Move(IWin32Window owner, IReadOnlyList<(string Source, string Folder)> moves) =>
        Run(owner, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR | FOFX_ADDUNDORECORD, [.. moves.Select(move => new Work(move.Source, Folder: move.Folder))], Strings.ToolMoveFailed, out _);

    /// <summary>
    /// Sends the items to the Recycle Bin, all in one operation, without Windows' own "are you
    /// sure": the window asked already. What cannot go there (a network drive, a file too large
    /// for the bin) is not deleted without Windows asking first.
    /// </summary>
    public static List<string> Recycle(IWin32Window owner, IReadOnlyList<string> paths) => Recycle(owner, paths, out _);

    /// <param name="aborted">The user cancelled, or skipped an item: not everything went.</param>
    public static List<string> Recycle(IWin32Window owner, IReadOnlyList<string> paths, out bool aborted) =>
        Run(owner, FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_WANTNUKEWARNING, [.. paths.Select(path => new Work(path))], Strings.ToolRecycleFailed, out aborted);

    /// <summary>
    /// Renames the items, all in one operation and in the order given (ADR-0029): a name is
    /// freed before another item takes it. The engine's undo record takes them all back.
    /// </summary>
    public static List<string> Rename(IWin32Window owner, IReadOnlyList<(string Path, string NewName)> renames) =>
        Run(owner, FOF_ALLOWUNDO | FOFX_ADDUNDORECORD, [.. renames.Select(rename => new Work(rename.Path, NewName: rename.NewName))], Strings.ToolRenameFailed, out _);

    /// <summary>
    /// Copies each item into its folder, all in one operation. Windows' questions stay: a file
    /// that would lose its encryption on the way is asked about (ADR-0036).
    /// </summary>
    /// <param name="aborted">The user cancelled, or skipped an item: not everything was copied.</param>
    public static List<string> Copy(IWin32Window owner, IReadOnlyList<(string Source, string Folder)> copies, out bool aborted) =>
        Run(owner, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR | FOFX_ADDUNDORECORD,
            [.. copies.Select(copy => new Work(copy.Source, Folder: copy.Folder, Copy: true))], Strings.ToolFileOperationFailed, out aborted);

    /// <summary>One item's part of the operation: moved or copied into <see cref="Folder"/>, given <see cref="NewName"/>, or deleted.</summary>
    private sealed record Work(string Source, string? Folder = null, string? NewName = null, bool Copy = false);

    private static List<string> Run(IWin32Window owner, uint flags, IReadOnlyList<Work> work, Func<string, string> failed, out bool aborted)
    {
        aborted = false;
        var errors = new List<string>();
        Guid clsid = s_fileOperation;
        Guid iid = typeof(IFileOperation).GUID;
        int hresult = CoCreateInstance(&clsid, 0, CLSCTX_INPROC_SERVER, &iid, out nint pointer);
        if (hresult < 0 || pointer == 0)
        {
            errors.Add(failed(Describe(hresult)));
            return errors;
        }

        var operation = (IFileOperation)s_wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pointer);
        var items = new List<nint>();
        var folders = new Dictionary<string, nint>(StringComparer.OrdinalIgnoreCase);
        try
        {
            // Replaces the default flags entirely.
            hresult = operation.SetOperationFlags(flags);
            if (hresult < 0)
            {
                errors.Add(failed(Describe(hresult)));
                return errors;
            }

            // Windows' progress window and questions get this window as their owner.
            operation.SetOwnerWindow(owner.Handle);
            int queued = 0;
            foreach (Work part in work)
            {
                nint item = ItemFromPath(part.Source, out hresult);
                if (item == 0)
                {
                    errors.Add(Strings.ToolItemFailed(part.Source, Describe(hresult)));
                    continue;
                }

                // Kept until the operation is done, as the engine expects.
                items.Add(item);
                if (part.Folder is { } folder)
                {
                    if (!folders.TryGetValue(folder, out nint target))
                    {
                        target = ItemFromPath(folder, out hresult);
                        if (target == 0)
                        {
                            errors.Add(Strings.ToolItemFailed(part.Source, Describe(hresult)));
                            continue;
                        }

                        folders[folder] = target;
                    }

                    hresult = part.Copy ? operation.CopyItem(item, target, null, 0) : operation.MoveItem(item, target, null, 0);
                }
                else if (part.NewName is { } newName)
                {
                    fixed (char* name = newName)
                    {
                        hresult = operation.RenameItem(item, name, 0);
                    }
                }
                else
                {
                    hresult = operation.DeleteItem(item, 0);
                }

                if (hresult < 0)
                {
                    errors.Add(Strings.ToolItemFailed(part.Source, Describe(hresult)));
                    continue;
                }

                queued++;
            }

            // With nothing queued the engine fails instead of doing nothing.
            if (queued == 0)
            {
                return errors;
            }

            // A cancel, or a skip in Windows' own question, is the user's answer, not a failure;
            // the operation may still report success for one, so it is asked as well.
            hresult = operation.PerformOperations();
            int anyAborted = 0;
            aborted = hresult is HRESULT_CANCELLED or COPYENGINE_E_USER_CANCELLED
                || (operation.GetAnyOperationsAborted(&anyAborted) >= 0 && anyAborted != 0);
            if (hresult < 0 && hresult is not HRESULT_CANCELLED and not COPYENGINE_E_USER_CANCELLED)
            {
                errors.Add(failed(Describe(hresult)));
            }

            return errors;
        }
        finally
        {
            foreach (nint item in items.Concat(folders.Values))
            {
                Marshal.Release(item);
            }

            ((ComObject)(object)operation).FinalRelease();
        }
    }

    /// <summary>An IShellItem for a path; 0 when the shell cannot parse it (gone, no access).</summary>
    private static nint ItemFromPath(string path, out int hresult)
    {
        Guid iid = s_shellItem;
        hresult = SHCreateItemFromParsingName(path, 0, &iid, out nint item);
        return hresult < 0 ? 0 : item;
    }

    /// <summary>Windows' text for an HRESULT, with the code for anything it has no text for.</summary>
    private static string Describe(int hresult) =>
        string.Create(CultureInfo.InvariantCulture, $"{new Win32Exception(hresult).Message} (0x{hresult:X8})");

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(Guid* clsid, nint outer, uint context, Guid* iid, out nint instance);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromParsingName(string path, nint bindContext, Guid* iid, out nint item);
}

/// <summary>
/// shobjidl_core.h, in its order: the generated stubs call by position. Blittable types only;
/// shell items are passed as the raw pointers the caller owns.
/// </summary>
[GeneratedComInterface]
[Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8")]
internal unsafe partial interface IFileOperation
{
    [PreserveSig]
    int Advise(nint sink, uint* cookie);

    [PreserveSig]
    int Unadvise(uint cookie);

    [PreserveSig]
    int SetOperationFlags(uint flags);

    [PreserveSig]
    int SetProgressMessage(char* message);

    [PreserveSig]
    int SetProgressDialog(nint dialog);

    [PreserveSig]
    int SetProperties(nint properties);

    [PreserveSig]
    int SetOwnerWindow(nint owner);

    [PreserveSig]
    int ApplyPropertiesToItem(nint item);

    [PreserveSig]
    int ApplyPropertiesToItems(nint items);

    [PreserveSig]
    int RenameItem(nint item, char* newName, nint sink);

    [PreserveSig]
    int RenameItems(nint items, char* newName);

    [PreserveSig]
    int MoveItem(nint item, nint destinationFolder, char* newName, nint sink);

    [PreserveSig]
    int MoveItems(nint items, nint destinationFolder);

    [PreserveSig]
    int CopyItem(nint item, nint destinationFolder, char* copyName, nint sink);

    [PreserveSig]
    int CopyItems(nint items, nint destinationFolder);

    [PreserveSig]
    int DeleteItem(nint item, nint sink);

    [PreserveSig]
    int DeleteItems(nint items);

    [PreserveSig]
    int NewItem(nint destinationFolder, uint attributes, char* name, char* templateName, nint sink);

    [PreserveSig]
    int PerformOperations();

    [PreserveSig]
    int GetAnyOperationsAborted(int* aborted);
}
