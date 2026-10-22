using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>
/// A file encrypted with a password (ADR-0032): AES-256-GCM in chunks, the key from the password
/// with PBKDF2-HMAC-SHA256. The layout, little-endian:
/// <code>
/// 0   8  "FBCENC", version 1, 0
/// 8   4  PBKDF2 iterations
/// 12  16 salt
/// 28  4  chunk size (plain bytes in every chunk but the last)
/// 32  7  nonce prefix, random for the file
/// 39  …  chunks: cipher text, then its 16-byte tag
/// </code>
/// A chunk's nonce is the prefix, its number (big-endian) and a byte that is 1 for the last
/// chunk only; the header is the associated data of every chunk. So a changed header, chunks
/// swapped, and a file cut short at a chunk's end all fail as a wrong password does. An empty
/// file is one empty last chunk.
/// </summary>
public static class FileCrypto
{
    /// <summary>What an encrypted file's name ends in.</summary>
    public const string Extension = ".fbcenc";

    /// <summary>OWASP's figure for PBKDF2-HMAC-SHA256 (2023).</summary>
    public const int Iterations = 600_000;

    // A header asking for more is not one of ours, or one meant to keep the computer busy.
    private const int MaxIterations = 10_000_000;

    private const int ChunkSize = 1024 * 1024;
    private const int MaxChunkSize = 16 * 1024 * 1024;
    private const int TagSize = 16;
    private const int SaltSize = 16;
    private const int PrefixSize = 7;
    private const int HeaderSize = 39;
    private static readonly byte[] s_magic = [(byte)'F', (byte)'B', (byte)'C', (byte)'E', (byte)'N', (byte)'C', 1, 0];

    /// <summary>The key for a batch of files: derived once, the slow part; each file still has its own nonces.</summary>
    public sealed class Key : IDisposable
    {
        internal Key(byte[] bytes, byte[] salt, int iterations)
        {
            Bytes = bytes;
            Salt = salt;
            Iterations = iterations;
        }

        internal byte[] Bytes { get; }

        internal byte[] Salt { get; }

        internal int Iterations { get; }

        public void Dispose() => CryptographicOperations.ZeroMemory(Bytes);
    }

    /// <summary>A new key from <paramref name="password"/> and a random salt.</summary>
    /// <param name="iterations">Fewer only for tests: every file records the number it was made with.</param>
    public static Key DeriveKey(string password, int iterations = Iterations) =>
        Derive(password, RandomNumberGenerator.GetBytes(SaltSize), iterations);

    private static Key Derive(string password, byte[] salt, int iterations) =>
        new(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password.Normalize(NormalizationForm.FormC)), salt, iterations, HashAlgorithmName.SHA256, 32),
            salt, iterations);

    /// <summary>Whether the file starts like one of ours.</summary>
    public static bool IsEncrypted(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Span<byte> magic = stackalloc byte[8];
            return stream.ReadAtLeast(magic, magic.Length, throwOnEndOfStream: false) == magic.Length && magic.SequenceEqual(s_magic);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Writes <paramref name="source"/> encrypted to <paramref name="destination"/>, a new file.</summary>
    /// <exception cref="IOException">Reading or writing failed; nothing is left at the destination.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused; nothing is left at the destination.</exception>
    public static void Encrypt(string source, string destination, Key key, CancellationToken cancellationToken)
    {
        byte[] header = new byte[HeaderSize];
        s_magic.CopyTo(header, 0);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), key.Iterations);
        key.Salt.CopyTo(header, 12);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(28), ChunkSize);
        RandomNumberGenerator.Fill(header.AsSpan(32, PrefixSize));

        Write(destination, output =>
        {
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var aes = new AesGcm(key.Bytes, TagSize);
            output.Write(header);
            byte[] plain = new byte[ChunkSize];
            byte[] cipher = new byte[ChunkSize];
            byte[] tag = new byte[TagSize];
            byte[] nonce = new byte[12];
            header.AsSpan(32, PrefixSize).CopyTo(nonce);
            int length = input.ReadAtLeast(plain, ChunkSize, throwOnEndOfStream: false);
            for (uint number = 0; ; number++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // A full chunk is the last only when nothing follows it.
                int next = length == ChunkSize ? input.ReadByte() : -1;
                bool last = next < 0;
                SetNonce(nonce, number, last);
                aes.Encrypt(nonce, plain.AsSpan(0, length), cipher.AsSpan(0, length), tag, header);
                output.Write(cipher, 0, length);
                output.Write(tag);
                if (last)
                {
                    break;
                }

                plain[0] = (byte)next;
                length = 1 + input.ReadAtLeast(plain.AsSpan(1), ChunkSize - 1, throwOnEndOfStream: false);
            }

            CryptographicOperations.ZeroMemory(plain);
        });
    }

    /// <summary>Writes the decrypted <paramref name="source"/> to <paramref name="destination"/>, a new file.</summary>
    /// <param name="keys">Keys already derived, by salt and iterations: files encrypted together share one.</param>
    /// <exception cref="ToolFailedException">Not one of ours, a wrong password, or a changed file; nothing is left at the destination.</exception>
    /// <exception cref="IOException">Reading or writing failed; nothing is left at the destination.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused; nothing is left at the destination.</exception>
    public static void Decrypt(string source, string destination, string password, Dictionary<string, Key> keys, CancellationToken cancellationToken)
    {
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] header = new byte[HeaderSize];
        int iterations;
        int chunkSize;
        if (input.ReadAtLeast(header, HeaderSize, throwOnEndOfStream: false) != HeaderSize
            || !header.AsSpan(0, 8).SequenceEqual(s_magic)
            || (iterations = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8))) is < 1 or > MaxIterations
            || (chunkSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(28))) is < 1 or > MaxChunkSize)
        {
            throw new ToolFailedException(Strings.CryptNotOurs());
        }

        byte[] salt = header[12..28];
        string id = $"{iterations}:{Convert.ToHexString(salt)}";
        if (!keys.TryGetValue(id, out Key? key))
        {
            key = Derive(password, salt, iterations);
            keys[id] = key;
        }

        Write(destination, output =>
        {
            using var aes = new AesGcm(key.Bytes, TagSize);
            byte[] cipher = new byte[chunkSize + TagSize];
            byte[] plain = new byte[chunkSize];
            byte[] nonce = new byte[12];
            header.AsSpan(32, PrefixSize).CopyTo(nonce);
            for (uint number = 0; ; number++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int length = input.ReadAtLeast(cipher, cipher.Length, throwOnEndOfStream: false);
                if (length < TagSize)
                {
                    throw new ToolFailedException(Strings.CryptWrongPassword());
                }

                bool last = length < cipher.Length || input.Position == input.Length;
                SetNonce(nonce, number, last);
                int plainLength = length - TagSize;
                try
                {
                    aes.Decrypt(nonce, cipher.AsSpan(0, plainLength), cipher.AsSpan(plainLength, TagSize), plain.AsSpan(0, plainLength), header);
                }
                catch (AuthenticationTagMismatchException)
                {
                    throw new ToolFailedException(Strings.CryptWrongPassword());
                }

                output.Write(plain, 0, plainLength);
                if (last)
                {
                    break;
                }
            }

            CryptographicOperations.ZeroMemory(plain);
        });
    }

    private static void SetNonce(byte[] nonce, uint number, bool last)
    {
        BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(PrefixSize), number);
        nonce[11] = last ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// A new file, written whole or not at all. It is written under a temporary name, flushed to
    /// the disk and only then given its name: an original deleted next is never gone while its
    /// replacement still sits in the cache, and a failed write leaves nothing under the real name.
    /// A file that was there before is never touched.
    /// </summary>
    private static void Write(string destination, Action<FileStream> write)
    {
        if (File.Exists(destination) || Directory.Exists(destination))
        {
            throw new IOException(Strings.CryptDestinationTaken(destination));
        }

        string temporary = destination + $".{Guid.NewGuid():N}.fbctools-tmp";
        bool done = false;
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                write(output);
                output.Flush(flushToDisk: true);
            }

            // No overwrite: a file that turned up meanwhile stays, and this one is not written.
            File.Move(temporary, destination);
            done = true;
        }
        finally
        {
            if (!done)
            {
                try
                {
                    File.Delete(temporary);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The failure to report is the one that brought us here; this name says what it is.
                }
            }
        }
    }

    /// <summary>
    /// Windows' "came from the internet" mark (the Zone.Identifier stream) of <paramref name="source"/>,
    /// put on <paramref name="destination"/> too: a document decrypted from a downloaded file must
    /// still open in Office's Protected View. Nothing on other systems, nothing when there is none.
    /// </summary>
    public static void CopyZone(string source, string destination)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            string zone = source + ":Zone.Identifier";
            if (File.Exists(zone))
            {
                File.WriteAllBytes(destination + ":Zone.Identifier", File.ReadAllBytes(zone));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // A volume without streams (FAT) keeps no mark either way.
        }
    }
}
