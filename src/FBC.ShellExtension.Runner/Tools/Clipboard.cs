using System.ComponentModel;
using System.Runtime.InteropServices;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>Puts Unicode text on the clipboard with plain Win32; the runner has no WinForms.</summary>
internal static unsafe class Clipboard
{
    private const int OpenAttempts = 10;
    /// <summary>What <see cref="GetText"/> reads at most: a checksum is short.</summary>
    public const int ChecksumChars = 64 * 1024;

    /// <summary>What append reads at most, in characters; more is refused, not cut.</summary>
    public const int TextChars = 16 * 1024 * 1024;

    private static readonly TimeSpan s_retryDelay = TimeSpan.FromMilliseconds(50);

    /// <exception cref="ClipboardBusyException">Another program kept the clipboard open.</exception>
    /// <exception cref="Win32Exception">Windows refused the text.</exception>
    public static void SetText(string text)
    {
        // EmptyClipboard makes the window that opened the clipboard its owner; with no window
        // SetClipboardData may fail. A message-only window is enough and is never shown.
        nint owner = CreateWindowExW(0, "STATIC", null, 0, 0, 0, 0, 0, HWND_MESSAGE, 0, 0, 0);
        if (owner == 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        try
        {
            if (!Open(owner))
            {
                throw new ClipboardBusyException();
            }

            try
            {
                if (!EmptyClipboard())
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                nint memory = Copy(text);
                if (SetClipboardData(CF_UNICODETEXT, memory) == 0)
                {
                    int error = Marshal.GetLastPInvokeError();
                    GlobalFree(memory);
                    throw new Win32Exception(error);
                }

                // The memory belongs to the system now and outlives this process.
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            DestroyWindow(owner);
        }
    }

    /// <summary>
    /// The clipboard's text, at most <paramref name="maxChars"/> of it (never half a surrogate
    /// pair); null when it holds none or cannot be opened. <paramref name="cut"/> says the rest
    /// was left out.
    /// </summary>
    public static string? GetText(int maxChars, out bool cut)
    {
        cut = false;
        if (!IsClipboardFormatAvailable(CF_UNICODETEXT) || !Open(0))
        {
            return null;
        }

        try
        {
            nint memory = GetClipboardData(CF_UNICODETEXT);
            char* text = memory == 0 ? null : (char*)GlobalLock(memory);
            if (text == null)
            {
                return null;
            }

            try
            {
                // A huge clipboard is cut rather than copied whole.
                ReadOnlySpan<char> all = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text);
                if (all.Length <= maxChars)
                {
                    return all.ToString();
                }

                cut = true;
                int length = char.IsHighSurrogate(all[maxChars - 1]) ? maxChars - 1 : maxChars;
                return all[..length].ToString();
            }
            finally
            {
                GlobalUnlock(memory);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool Open(nint owner)
    {
        // A clipboard manager or a remote desktop client may hold the clipboard for a moment.
        for (int attempt = 1; ; attempt++)
        {
            if (OpenClipboard(owner))
            {
                return true;
            }

            if (attempt == OpenAttempts)
            {
                return false;
            }

            Thread.Sleep(s_retryDelay);
        }
    }

    private static nint Copy(string text)
    {
        nuint size = (nuint)(text.Length + 1) * sizeof(char);
        nint memory = GlobalAlloc(GMEM_MOVEABLE, size);
        if (memory == 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        char* target = (char*)GlobalLock(memory);
        if (target == null)
        {
            int error = Marshal.GetLastPInvokeError();
            GlobalFree(memory);
            throw new Win32Exception(error);
        }

        text.AsSpan().CopyTo(new Span<char>(target, text.Length));
        target[text.Length] = '\0';
        GlobalUnlock(memory);
        return memory;
    }
}

internal sealed class ClipboardBusyException() : Exception("The clipboard is in use by another program.");
