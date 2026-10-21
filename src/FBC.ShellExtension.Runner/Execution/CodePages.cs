using System.Text;
using FBC.ShellExtension.Runner.Interop;

namespace FBC.ShellExtension.Runner.Execution;

/// <summary>The system's OEM and Windows code pages, for bytes written without saying how (G-0104).</summary>
internal static class CodePages
{
    public static Encoding Oem { get; } = Get(Native.GetOEMCP());

    public static Encoding Ansi { get; } = Get(Native.GetACP());

    private static Encoding Get(uint codePage)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            return Encoding.GetEncoding((int)codePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
    }
}
