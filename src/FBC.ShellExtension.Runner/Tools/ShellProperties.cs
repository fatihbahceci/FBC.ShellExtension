using System.Runtime.InteropServices;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Reads a file's property the way Explorer's Details pane does, through the shell's property
/// store and the file type's property handler (an MP3's tags, an Office document's summary).
/// Raw COM through the vtable, as NativeAOT has no built-in COM interop (ADR-0019).
/// </summary>
internal static unsafe class ShellProperties
{
    // IPropertyStore: IUnknown (0-2), GetCount, GetAt, GetValue (5).
    private const int ReleaseSlot = 2;
    private const int GetValueSlot = 5;

    private static readonly Guid s_propertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

    // PKEY_Title, System.Title.
    private static readonly PropertyKey s_title = new(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2);

    /// <summary>The "Title" property; null when the file has none or Windows cannot read it.</summary>
    public static string? Title(string path)
    {
        // Property handlers expect a single-threaded apartment; balanced here, whatever was there.
        int initialized = CoInitializeEx(0, COINIT_APARTMENTTHREADED);
        try
        {
            Guid iid = s_propertyStore;
            nint store;
            if (SHGetPropertyStoreFromParsingName(path, 0, GPS_BESTEFFORT, &iid, &store) < 0 || store == 0)
            {
                return null;
            }

            try
            {
                nint* vtable = *(nint**)store;
                var getValue = (delegate* unmanaged<nint, PropertyKey*, PropVariant*, int>)vtable[GetValueSlot];
                PropertyKey key = s_title;
                PropVariant value = default;
                if (getValue(store, &key, &value) < 0)
                {
                    return null;
                }

                try
                {
                    return value.Type is VT_LPWSTR or VT_BSTR && value.Pointer != 0
                        ? new string((char*)value.Pointer).Trim()
                        : null;
                }
                finally
                {
                    PropVariantClear(&value);
                }
            }
            finally
            {
                ((delegate* unmanaged<nint, uint>)(*(nint**)store)[ReleaseSlot])(store);
            }
        }
        finally
        {
            if (initialized >= 0)
            {
                CoUninitialize();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public readonly Guid FormatId = formatId;
        public readonly uint PropertyId = propertyId;
    }

    /// <summary>PROPVARIANT on x64: the type, three reserved words, then the value (a pointer for strings).</summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PropVariant
    {
        [FieldOffset(0)]
        public ushort Type;

        [FieldOffset(8)]
        public nint Pointer;
    }
}
