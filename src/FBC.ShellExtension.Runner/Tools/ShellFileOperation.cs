using FBC.ShellExtension.Core.Resources;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Copies or moves with Explorer's own engine (IFileOperation): its progress window, its
/// question when a name is taken, elevation when a folder needs it, and an undo record that
/// Ctrl+Z in Explorer reverses (ADR-0026). Windows reports what went wrong with a file in its
/// own dialogs; what is left here is what kept the operation from starting.
/// </summary>
internal static unsafe class ShellFileOperation
{
    private static readonly Guid s_fileOperation = new("3AD05575-8857-4850-9277-11B85BDB8E09");
    private static readonly Guid s_iFileOperation = new("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8");

    // IFileOperation: IUnknown (0-2), Advise, Unadvise, SetOperationFlags (5) …
    private const int SetOperationFlagsSlot = 5;
    private const int MoveItemSlot = 14;
    private const int CopyItemSlot = 16;
    private const int PerformOperationsSlot = 21;

    /// <summary>Copies or moves <paramref name="sources"/> into <paramref name="destination"/>, all in one operation.</summary>
    /// <returns>A line for each item that could not be handed over, and for an operation that failed as a whole.</returns>
    public static List<string> Run(bool move, IReadOnlyList<string> sources, string destination)
    {
        var errors = new List<string>();
        using var apartment = new ShellCom.Apartment();
        Guid clsid = s_fileOperation;
        Guid iid = s_iFileOperation;
        nint operation = 0;
        int hresult = CoCreateInstance(&clsid, 0, CLSCTX_INPROC_SERVER, &iid, &operation);
        if (hresult < 0 || operation == 0)
        {
            errors.Add(Strings.ToolFileOperationFailed(ShellCom.Describe(hresult)));
            return errors;
        }

        var items = new List<nint>();
        nint target = 0;
        try
        {
            nint* vtable = *(nint**)operation;

            // Replaces the default flags (FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR) entirely.
            hresult = ((delegate* unmanaged<nint, uint, int>)vtable[SetOperationFlagsSlot])(operation, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR | FOFX_ADDUNDORECORD);
            if (hresult < 0)
            {
                errors.Add(Strings.ToolFileOperationFailed(ShellCom.Describe(hresult)));
                return errors;
            }

            target = ShellCom.ItemFromPath(destination, out hresult);
            if (target == 0)
            {
                errors.Add(Strings.ToolItemFailed(destination, ShellCom.Describe(hresult)));
                return errors;
            }

            var queue = (delegate* unmanaged<nint, nint, nint, char*, nint, int>)vtable[move ? MoveItemSlot : CopyItemSlot];
            int queued = 0;
            foreach (string source in sources)
            {
                nint item = ShellCom.ItemFromPath(source, out hresult);
                if (item == 0)
                {
                    errors.Add(Strings.ToolItemFailed(source, ShellCom.Describe(hresult)));
                    continue;
                }

                // Kept until the operation is done; the same name, no progress sink of our own.
                items.Add(item);
                hresult = queue(operation, item, target, null, 0);
                if (hresult < 0)
                {
                    errors.Add(Strings.ToolItemFailed(source, ShellCom.Describe(hresult)));
                    continue;
                }

                queued++;
            }

            // With nothing queued the engine fails instead of doing nothing.
            if (queued == 0)
            {
                return errors;
            }

            hresult = ((delegate* unmanaged<nint, int>)vtable[PerformOperationsSlot])(operation);

            // A cancel, or a skip in Windows' own question, is the user's answer, not a failure.
            if (hresult < 0 && hresult is not HRESULT_CANCELLED and not COPYENGINE_E_USER_CANCELLED)
            {
                errors.Add(Strings.ToolFileOperationFailed(ShellCom.Describe(hresult)));
            }

            return errors;
        }
        finally
        {
            foreach (nint item in items)
            {
                ShellCom.Release(item);
            }

            ShellCom.Release(target);
            ShellCom.Release(operation);
        }
    }
}
