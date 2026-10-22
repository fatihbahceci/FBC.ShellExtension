using System.Runtime.InteropServices;
using System.Text;

namespace FBC.ShellExtension.WikiImport;

/// <summary>
/// Encrypts an API key or token for the Windows user with DPAPI: only the same user on the same
/// machine can read it back (ADR-0020). The entropy keeps other programs' DPAPI blobs from passing
/// as ours. Windows only; the runner and the editor share it (ADR-0039).
/// </summary>
public sealed unsafe partial class DpapiProtector : ISecretProtector
{
    private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public uint cbData;
        public byte* pbData;
    }

    [LibraryImport("crypt32.dll", EntryPoint = "CryptProtectData", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptProtectData(DATA_BLOB* pDataIn, string? szDataDescr, DATA_BLOB* pOptionalEntropy,
        nint pvReserved, nint pPromptStruct, uint dwFlags, DATA_BLOB* pDataOut);

    [LibraryImport("crypt32.dll", EntryPoint = "CryptUnprotectData", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptUnprotectData(DATA_BLOB* pDataIn, nint ppszDataDescr, DATA_BLOB* pOptionalEntropy,
        nint pvReserved, nint pPromptStruct, uint dwFlags, DATA_BLOB* pDataOut);

    [LibraryImport("kernel32.dll", EntryPoint = "LocalFree")]
    private static partial nint LocalFree(nint hMem);
}
