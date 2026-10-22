using System.Runtime.InteropServices;
using System.Text;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// The Windows code page of this computer (1254 on a Turkish Windows), which Notepad reads a
/// file in when it is not UTF-8; .NET knows only Unicode encodings without the provider.
/// </summary>
internal static partial class CodePage
{
    public static Encoding Ansi { get; } = Create();

    private static Encoding Create()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            return Encoding.GetEncoding((int)GetACP());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.Latin1;
        }
    }

    [LibraryImport("kernel32.dll")]
    private static partial uint GetACP();
}
