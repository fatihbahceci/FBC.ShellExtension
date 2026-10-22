using System.ComponentModel;
using System.Globalization;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// What the shell's COM objects share: the apartment, shell items for paths, and releasing.
/// Raw COM through the vtable, as NativeAOT has no built-in COM interop (ADR-0019, ADR-0026).
/// Every object is created, used and released on the calling thread.
/// </summary>
internal static unsafe class ShellCom
{
    public static readonly Guid IShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");

    // IUnknown and IShellItem slots.
    private const int ReleaseSlot = 2;
    private const int GetDisplayNameSlot = 5;

    /// <summary>
    /// A single-threaded apartment for the dialogs and the copy engine, which work only there.
    /// The runner's main thread is one already ([STAThread]); the call then returns S_FALSE,
    /// which is balanced all the same.
    /// </summary>
    public sealed class Apartment : IDisposable
    {
        private readonly bool _initialized = CoInitializeEx(0, COINIT_APARTMENTTHREADED) >= 0;

        public void Dispose()
        {
            if (_initialized)
            {
                CoUninitialize();
            }
        }
    }

    public static void Release(nint unknown)
    {
        if (unknown != 0)
        {
            ((delegate* unmanaged<nint, uint>)(*(nint**)unknown)[ReleaseSlot])(unknown);
        }
    }

    /// <summary>An IShellItem for a path; 0 when the shell cannot parse it (gone, no access).</summary>
    public static nint ItemFromPath(string path, out int hresult)
    {
        Guid iid = IShellItem;
        nint item = 0;
        fixed (char* text = path)
        {
            hresult = SHCreateItemFromParsingName(text, 0, &iid, &item);
        }

        return hresult < 0 ? 0 : item;
    }

    /// <summary>The item's file system path; null for an item that has none.</summary>
    public static string? FileSystemPath(nint item)
    {
        char* name = null;
        int hresult = ((delegate* unmanaged<nint, int, char**, int>)(*(nint**)item)[GetDisplayNameSlot])(item, SIGDN_FILESYSPATH, &name);
        try
        {
            return hresult >= 0 && name != null ? new string(name) : null;
        }
        finally
        {
            CoTaskMemFree((nint)name);
        }
    }

    /// <summary>Windows' text for an HRESULT, with the code for anything it has no text for.</summary>
    public static string Describe(int hresult) =>
        string.Create(CultureInfo.InvariantCulture, $"{new Win32Exception(hresult).Message} (0x{hresult:X8})");
}
