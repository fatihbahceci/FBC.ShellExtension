using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// When a photo was taken or a video recorded, read the way Explorer's Details pane reads it:
/// the shell's property store and the file type's property handler (ADR-0028).
/// </summary>
internal static unsafe partial class ShellDates
{
    private const int GPS_BESTEFFORT = 0x40;
    private const ushort VT_FILETIME = 64;

    private static readonly Guid s_propertyStore = typeof(IPropertyStore).GUID;
    private static readonly StrategyBasedComWrappers s_wrappers = new();

    // PKEY_Photo_DateTaken (EXIF DateTimeOriginal), then PKEY_Media_DateEncoded for videos.
    private static readonly PropertyKey[] s_keys =
    [
        new(new Guid("14B81DA1-0135-4D31-96D9-6CBFC9671A99"), 36867),
        new(new Guid("2E4B640D-5019-46D8-8881-55414CC5CAA0"), 100),
    ];

    /// <summary>
    /// The date each file was taken; the modified date for one that has none. Read on a thread
    /// of its own: property handlers expect a single-threaded apartment, and a thousand photos
    /// take a while.
    /// </summary>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static Task<Dictionary<string, DateTime>> TakenAsync(IReadOnlyList<string> files, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<Dictionary<string, DateTime>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var dates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    dates[file] = TakenOrNull(file) ?? Modified(file);
                }

                completion.SetResult(dates);
            }
            catch (OperationCanceledException)
            {
                completion.SetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "Dates",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static DateTime Modified(string file)
    {
        try
        {
            return FileTools.FileDate(file, OrganizeDate.Modified);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Gone meanwhile: moving it will say so.
            return DateTime.Now;
        }
    }

    /// <summary>A property handler is the file type's own code, in this process: whatever it throws costs the file its date, not the list.</summary>
    private static DateTime? TakenOrNull(string file)
    {
        try
        {
            return Taken(file);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or ArgumentException)
        {
            return null;
        }
    }

    private static DateTime? Taken(string file)
    {
        Guid iid = s_propertyStore;
        if (SHGetPropertyStoreFromParsingName(file, 0, GPS_BESTEFFORT, &iid, out nint pointer) < 0 || pointer == 0)
        {
            return null;
        }

        var store = (IPropertyStore)s_wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pointer);
        try
        {
            foreach (PropertyKey key in s_keys)
            {
                PropertyKey copy = key;
                PropVariant value = default;
                if (store.GetValue(&copy, &value) < 0)
                {
                    continue;
                }

                try
                {
                    // Stored in UTC, as a FILETIME; the folders go by the local date, as Explorer shows it.
                    if (value.Type == VT_FILETIME && value.FileTime > 0)
                    {
                        return DateTime.FromFileTime(value.FileTime);
                    }
                }
                finally
                {
                    PropVariantClear(&value);
                }
            }

            return null;
        }
        finally
        {
            ((ComObject)(object)store).FinalRelease();
        }
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHGetPropertyStoreFromParsingName(string path, nint bindContext, int flags, Guid* iid, out nint store);

    [LibraryImport("ole32.dll")]
    private static partial int PropVariantClear(PropVariant* value);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public readonly Guid FormatId = formatId;
        public readonly uint PropertyId = propertyId;
    }

    /// <summary>PROPVARIANT on x64: the type, three reserved words, then the value (a FILETIME here).</summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct PropVariant
    {
        [FieldOffset(0)]
        public ushort Type;

        [FieldOffset(8)]
        public long FileTime;
    }

    /// <summary>propsys.h, in its order.</summary>
    [GeneratedComInterface]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    internal partial interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(uint* count);

        [PreserveSig]
        int GetAt(uint index, PropertyKey* key);

        [PreserveSig]
        int GetValue(PropertyKey* key, PropVariant* value);

        [PreserveSig]
        int SetValue(PropertyKey* key, PropVariant* value);

        [PreserveSig]
        int Commit();
    }
}
