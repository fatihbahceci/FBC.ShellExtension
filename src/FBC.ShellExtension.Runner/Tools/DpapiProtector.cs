using System.Text;
using FBC.ShellExtension.WikiImport;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Encrypts an API key for the Windows user with DPAPI: only the same user on the same machine
/// can read it back (ADR-0020). The entropy keeps other programs' DPAPI blobs from passing as ours.
/// </summary>
internal sealed unsafe class DpapiProtector : ISecretProtector
{
    private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("FBCTools.WikiImport");

    public string Protect(string secret) =>
        Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(secret), protect: true)
            ?? throw new InvalidOperationException("CryptProtectData failed."));

    public string? Unprotect(string protectedText)
    {
        try
        {
            return Transform(Convert.FromBase64String(protectedText), protect: false) is { } bytes ? Encoding.UTF8.GetString(bytes) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static byte[]? Transform(byte[] input, bool protect)
    {
        fixed (byte* data = input)
        fixed (byte* entropy = s_entropy)
        {
            var inBlob = new DATA_BLOB { cbData = (uint)input.Length, pbData = data };
            var entropyBlob = new DATA_BLOB { cbData = (uint)s_entropy.Length, pbData = entropy };
            DATA_BLOB outBlob = default;
            bool done = protect
                ? CryptProtectData(&inBlob, "FBCTools", &entropyBlob, 0, 0, CRYPTPROTECT_UI_FORBIDDEN, &outBlob)
                : CryptUnprotectData(&inBlob, 0, &entropyBlob, 0, 0, CRYPTPROTECT_UI_FORBIDDEN, &outBlob);
            if (!done)
            {
                return null;
            }

            try
            {
                return new ReadOnlySpan<byte>(outBlob.pbData, (int)outBlob.cbData).ToArray();
            }
            finally
            {
                LocalFree((nint)outBlob.pbData);
            }
        }
    }
}
