using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>Encrypt and Decrypt with a password, file by file (ADR-0032); Windows' own EFS is the editor's.</summary>
public static partial class FileTools
{
    /// <summary>
    /// The files to encrypt or decrypt: the selected files, and the files in the selected folders
    /// and their subfolders. Hidden and system files (desktop.ini) are left out, links not entered.
    /// </summary>
    /// <param name="errors">A line for each folder that could not be read.</param>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static IReadOnlyList<string> CryptSources(IReadOnlyList<SelectedItem> items, out List<string> errors, CancellationToken cancellationToken) =>
        ReplaceSources(items, ["*"], subfolders: true, out errors, cancellationToken);

    /// <summary>
    /// The selected folders and their subfolders, for Windows' EFS: a folder marked encrypted
    /// encrypts the files made in it later. Hidden and system ones are left out, links not entered.
    /// </summary>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static IReadOnlyList<string> CryptFolders(IReadOnlyList<SelectedItem> items, CancellationToken cancellationToken)
    {
        var folders = new List<string>();
        var pending = new Stack<DirectoryInfo>(items.Where(item => item.Kind == ItemKind.Folder).Select(item => new DirectoryInfo(item.Path)).Reverse());
        while (pending.TryPop(out DirectoryInfo? folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            folders.Add(folder.FullName);
            try
            {
                foreach (DirectoryInfo subfolder in folder.GetDirectories("*", s_visible).Where(subfolder => !IsLink(subfolder)).Reverse())
                {
                    pending.Push(subfolder);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // CryptSources says so for the files; the folder itself is still marked.
            }
        }

        return folders;
    }

    /// <summary>
    /// Encrypts every file next to itself as <c>name.ext.fbcenc</c>. With
    /// <paramref name="deleteOriginals"/> the original is deleted, not sent to the Recycle Bin,
    /// which would keep it readable.
    /// </summary>
    /// <returns>A line for each file that was not encrypted, or whose original stayed.</returns>
    /// <exception cref="OperationCanceledException">Cancelled; the file being written is removed, the ones done stay.</exception>
    public static IReadOnlyList<string> EncryptFiles(IReadOnlyList<string> files, FileCrypto.Key key, bool deleteOriginals, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FileCrypto.IsEncrypted(file))
            {
                errors.Add(Strings.ToolItemFailed(file, Strings.CryptAlreadyEncrypted()));
                continue;
            }

            string folder = Path.GetDirectoryName(file) ?? "";
            string target = Path.Combine(folder, FreeEncryptedName(folder, Path.GetFileName(file)));
            try
            {
                FileCrypto.Encrypt(file, target, key, cancellationToken);
                FileCrypto.CopyZone(file, target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(file, ex.Message));
                continue;
            }

            if (deleteOriginals)
            {
                try
                {
                    DeleteForGood(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(file, Strings.CryptOriginalKept(ex.Message)));
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Decrypts every file next to itself, under its name without <c>.fbcenc</c> (numbered when
    /// that is taken). With <paramref name="deleteEncrypted"/> the encrypted file goes once its
    /// content is back.
    /// </summary>
    /// <returns>A line for each file that was not decrypted: not ours, a wrong password, a changed file.</returns>
    /// <exception cref="OperationCanceledException">Cancelled; the file being written is removed, the ones done stay.</exception>
    public static IReadOnlyList<string> DecryptFiles(IReadOnlyList<string> files, string password, bool deleteEncrypted, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var keys = new Dictionary<string, FileCrypto.Key>();
        try
        {
            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string folder = Path.GetDirectoryName(file) ?? "";
                string name = Path.GetFileName(file);
                string plainName = name.Length > FileCrypto.Extension.Length && name.EndsWith(FileCrypto.Extension, StringComparison.OrdinalIgnoreCase)
                    ? name[..^FileCrypto.Extension.Length]
                    : name + ".decrypted";
                string target = Path.Combine(folder, FreeItemName(folder, plainName, isFile: true));
                try
                {
                    FileCrypto.Decrypt(file, target, password, keys, cancellationToken);
                    FileCrypto.CopyZone(file, target);
                }
                catch (ToolFailedException ex)
                {
                    errors.Add(Strings.ToolItemFailed(file, ex.Message));
                    continue;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(file, ex.Message));
                    continue;
                }

                if (deleteEncrypted)
                {
                    try
                    {
                        DeleteForGood(file);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        errors.Add(Strings.ToolItemFailed(file, Strings.CryptEncryptedKept(ex.Message)));
                    }
                }
            }
        }
        finally
        {
            foreach (FileCrypto.Key key in keys.Values)
            {
                key.Dispose();
            }
        }

        return errors;
    }

    /// <summary>Deleted for good, a read-only file too: what it held now lives in the other file. Unchanged if it stays.</summary>
    private static void DeleteForGood(string path)
    {
        var info = new FileInfo(path);
        FileAttributes attributes = info.Attributes;
        info.Attributes = attributes & ~FileAttributes.ReadOnly;
        try
        {
            info.Delete();
        }
        catch
        {
            info.Attributes = attributes;
            throw;
        }
    }

    /// <summary>
    /// <c>report.pdf.fbcenc</c>, or <c>report (2).pdf.fbcenc</c> … when taken: the number goes
    /// where Decrypt keeps it, before the original extension.
    /// </summary>
    private static string FreeEncryptedName(string folder, string name)
    {
        string extension = WindowsPath.GetExtension(name);
        if (extension.Length == name.Length)
        {
            extension = "";
        }

        string stem = name[..^extension.Length];
        return Enumerable.Range(1, int.MaxValue - 1)
            .Select(number => (number == 1 ? name : $"{stem} ({number}){extension}") + FileCrypto.Extension)
            .First(candidate => NameProblem(folder, candidate) is null || !WindowsPath.IsValidName(candidate));
    }
}
