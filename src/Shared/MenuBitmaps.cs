using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Shared;

/// <summary>
/// Turns an entry's icon (a PNG, ADR-0041) into the bitmap a Win32 menu item shows: a 32bpp
/// top-down DIB with premultiplied alpha, at the menu's small-icon size. Decoded with Windows'
/// own codec (WIC), through raw vtable calls as NativeAOT has no built-in COM interop (ADR-0026).
/// The decoder is asked for by container and vendor (PNG, Microsoft's built-in one) rather than
/// guessed from the bytes, which would try every installed codec inside Explorer. The slots and
/// GUIDs are those of wincodec.idl. Compiled into the shell extension and, for the taskbar menu,
/// the runner. It needs a thread that has COM initialized.
/// </summary>
internal static unsafe partial class MenuBitmaps
{
    // A bitmap is made once per icon and size and kept for the life of the process: a menu does
    // not free its items' bitmaps, Explorer may still show one when the next is built, and the
    // shell extension is never unloaded. The count is capped so that a config edited all day
    // cannot exhaust GDI handles.
    private const int MaxBitmaps = 256;

    private static readonly Lock s_lock = new();
    private static readonly Dictionary<(string Png, int Size), nint> s_bitmaps = [];

    private static readonly Guid CLSID_WICImagingFactory1 = new("cacaf262-9370-4615-a13b-9f5539da4c0a");
    private static readonly Guid IID_IWICImagingFactory = new("ec5ec8a9-c395-4314-9c77-54d7a935ff70");
    private static readonly Guid GUID_ContainerFormatPng = new("1b7cfaf4-713f-473c-bbcd-6137425faeaf");
    private static readonly Guid GUID_VendorMicrosoftBuiltIn = new("257a30fd-06b6-462b-aea4-63f70b86e533");
    private static readonly Guid GUID_WICPixelFormat32bppPBGRA = new("6fddc324-4e03-4bfe-b185-3d77768dc910");

    // IUnknown
    private const int ReleaseSlot = 2;

    // IWICImagingFactory
    private const int CreateDecoderSlot = 7;
    private const int CreateFormatConverterSlot = 10;
    private const int CreateBitmapScalerSlot = 11;
    private const int CreateStreamSlot = 14;

    // IWICStream (IStream's eleven methods, then its own)
    private const int InitializeFromMemorySlot = 16;

    // IWICBitmapDecoder
    private const int DecoderInitializeSlot = 4;
    private const int GetFrameSlot = 13;

    // IWICBitmapSource, and its derived converter and scaler
    private const int CopyPixelsSlot = 7;
    private const int SourceInitializeSlot = 8;

    private const uint CLSCTX_INPROC_SERVER = 0x1;
    private const int WICDecodeMetadataCacheOnDemand = 0;
    private const int WICBitmapDitherTypeNone = 0;
    private const int WICBitmapPaletteTypeCustom = 0;
    private const int WICBitmapInterpolationModeFant = 3;
    private const uint DIB_RGB_COLORS = 0;
    private const int SM_CXSMICON = 49;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int MDT_EFFECTIVE_DPI = 0;
    private const int DPI_AWARENESS_UNAWARE = 0;
    private static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    /// <summary>
    /// The menu's small-icon size where the menu is about to open: 16 at 100%, 24 at 150%, 32 at
    /// 200%. A thread that is not DPI aware is drawn at 96 and stretched by Windows. Only a
    /// per-monitor v2 thread's menus follow the monitor they open on; any other aware thread's
    /// keep the system's metrics.
    /// </summary>
    public static int IconSize()
    {
        try
        {
            nint context = GetThreadDpiAwarenessContext();
            uint dpi = GetAwarenessFromDpiAwarenessContext(context) == DPI_AWARENESS_UNAWARE ? 96
                : AreDpiAwarenessContextsEqual(context, DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) ? MonitorDpi() ?? GetDpiForSystem()
                : GetDpiForSystem();
            int size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
            return size > 0 ? size : 16;
        }
        catch (EntryPointNotFoundException)
        {
            // Windows 10 before 1607.
            return 16;
        }
    }

    /// <summary>
    /// The DPI of the monitor the menu opens on. A right-click opens it at the cursor, over this
    /// thread's active window; Shift+F10 or the Menu key open it at the selected item, wherever
    /// the cursor is, so then the window's monitor counts. Null when neither can be read.
    /// </summary>
    private static uint? MonitorDpi()
    {
        var gui = new GUITHREADINFO { cbSize = (uint)sizeof(GUITHREADINFO) };
        nint window = GetGUIThreadInfo(GetCurrentThreadId(), &gui) ? (gui.hwndActive != 0 ? gui.hwndActive : gui.hwndFocus) : 0;
        nint monitor = 0;
        if (GetCursorPos(out POINT cursor)
            && (window == 0 || (GetWindowRect(window, out RECT frame)
                && cursor.X >= frame.Left && cursor.X < frame.Right && cursor.Y >= frame.Top && cursor.Y < frame.Bottom)))
        {
            monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
        }
        else if (window != 0)
        {
            monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
        }

        return monitor != 0 && GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpi, out _) >= 0 ? dpi : null;
    }

    /// <summary>The menu bitmap of <paramref name="png"/> at <paramref name="size"/>; 0 when it cannot be made, and why.</summary>
    public static nint For(byte[] png, int size, out string? problem)
    {
        problem = null;
        string key = Convert.ToBase64String(png);
        lock (s_lock)
        {
            if (s_bitmaps.TryGetValue((key, size), out nint made))
            {
                return made;
            }

            if (s_bitmaps.Count >= MaxBitmaps)
            {
                problem = $"more than {MaxBitmaps} icons";
                return 0;
            }

            nint bitmap = Create(png, size, out problem);
            if (bitmap != 0)
            {
                s_bitmaps[(key, size)] = bitmap;
            }

            return bitmap;
        }
    }

    private static nint Create(byte[] png, int size, out string? problem)
    {
        // The config's own checks, again: WIC is only ever asked to open a small PNG.
        if (MenuIcon.Problem(png) is { } invalid)
        {
            problem = invalid;
            return 0;
        }

        nint factory = 0, stream = 0, decoder = 0, frame = 0, converter = 0, scaler = 0, bitmap = 0;
        byte* buffer = (byte*)NativeMemory.Alloc((nuint)png.Length);
        try
        {
            png.CopyTo(new Span<byte>(buffer, png.Length));
            Guid clsid = CLSID_WICImagingFactory1, iid = IID_IWICImagingFactory, container = GUID_ContainerFormatPng,
                vendor = GUID_VendorMicrosoftBuiltIn, format = GUID_WICPixelFormat32bppPBGRA;

            int hr = CoCreateInstance(&clsid, 0, CLSCTX_INPROC_SERVER, &iid, &factory);
            if (hr < 0)
            {
                return Failed("CoCreateInstance", hr, out problem);
            }

            hr = ((delegate* unmanaged<nint, nint*, int>)Slot(factory, CreateStreamSlot))(factory, &stream);
            if (hr >= 0)
            {
                // The memory stays ours until everything that reads it is released, below.
                hr = ((delegate* unmanaged<nint, byte*, uint, int>)Slot(stream, InitializeFromMemorySlot))(stream, buffer, (uint)png.Length);
            }

            if (hr >= 0)
            {
                hr = ((delegate* unmanaged<nint, Guid*, Guid*, nint*, int>)Slot(factory, CreateDecoderSlot))(factory, &container, &vendor, &decoder);
            }

            if (hr >= 0)
            {
                hr = ((delegate* unmanaged<nint, nint, int, int>)Slot(decoder, DecoderInitializeSlot))(decoder, stream, WICDecodeMetadataCacheOnDemand);
            }

            if (hr >= 0)
            {
                hr = ((delegate* unmanaged<nint, uint, nint*, int>)Slot(decoder, GetFrameSlot))(decoder, 0, &frame);
            }

            if (hr < 0)
            {
                return Failed("decoding", hr, out problem);
            }

            // Premultiplied first, then scaled: averaging premultiplied pixels keeps edges clean.
            hr = ((delegate* unmanaged<nint, nint*, int>)Slot(factory, CreateFormatConverterSlot))(factory, &converter);
            if (hr >= 0)
            {
                hr = ((delegate* unmanaged<nint, nint, Guid*, int, nint, double, int, int>)Slot(converter, SourceInitializeSlot))(
                    converter, frame, &format, WICBitmapDitherTypeNone, 0, 0.0, WICBitmapPaletteTypeCustom);
            }

            if (hr >= 0)
            {
                hr = ((delegate* unmanaged<nint, nint*, int>)Slot(factory, CreateBitmapScalerSlot))(factory, &scaler);
            }

            if (hr >= 0)
            {
                hr = ((delegate* unmanaged<nint, nint, uint, uint, int, int>)Slot(scaler, SourceInitializeSlot))(
                    scaler, converter, (uint)size, (uint)size, WICBitmapInterpolationModeFant);
            }

            if (hr < 0)
            {
                return Failed("scaling", hr, out problem);
            }

            var info = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = size,
                biHeight = -size,
                biPlanes = 1,
                biBitCount = 32,
            };
            void* bits;
            bitmap = CreateDIBSection(0, &info, DIB_RGB_COLORS, &bits, 0, 0);
            if (bitmap == 0)
            {
                problem = "CreateDIBSection failed";
                return 0;
            }

            uint stride = (uint)size * 4;
            hr = ((delegate* unmanaged<nint, void*, uint, uint, void*, int>)Slot(scaler, CopyPixelsSlot))(scaler, null, stride, stride * (uint)size, bits);
            if (hr < 0)
            {
                DeleteObject(bitmap);
                bitmap = 0;
                return Failed("CopyPixels", hr, out problem);
            }

            problem = null;
            nint done = bitmap;
            bitmap = 0;
            return done;
        }
        finally
        {
            if (bitmap != 0)
            {
                DeleteObject(bitmap);
            }

            Release(scaler);
            Release(converter);
            Release(frame);
            Release(decoder);
            Release(stream);
            Release(factory);
            NativeMemory.Free(buffer);
        }
    }

    private static nint Failed(string step, int hr, out string problem)
    {
        problem = $"{step} failed: 0x{hr:X8}";
        return 0;
    }

    private static void* Slot(nint unknown, int slot) => (*(void***)unknown)[slot];

    private static void Release(nint unknown)
    {
        if (unknown != 0)
        {
            ((delegate* unmanaged<nint, uint>)Slot(unknown, ReleaseSlot))(unknown);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public uint cbSize;
        public uint flags;
        public nint hwndActive;
        public nint hwndFocus;
        public nint hwndCapture;
        public nint hwndMenuOwner;
        public nint hwndMoveSize;
        public nint hwndCaret;
        public RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(Guid* rclsid, nint pUnkOuter, uint dwClsContext, Guid* riid, nint* ppv);

    [LibraryImport("gdi32.dll")]
    private static partial nint CreateDIBSection(nint hdc, BITMAPINFOHEADER* pbmi, uint usage, void** ppvBits, nint hSection, uint offset);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint ho);

    [LibraryImport("user32.dll")]
    private static partial nint GetThreadDpiAwarenessContext();

    [LibraryImport("user32.dll")]
    private static partial int GetAwarenessFromDpiAwarenessContext(nint value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AreDpiAwarenessContextsEqual(nint dpiContextA, nint dpiContextB);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForSystem();

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetGUIThreadInfo(uint idThread, GUITHREADINFO* pgui);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetricsForDpi(int nIndex, uint dpi);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromPoint(POINT pt, uint dwFlags);

    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
