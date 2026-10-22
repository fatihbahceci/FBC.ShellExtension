using System.Text;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.WikiImport;

/// <summary>A local image the markdown links to, and the name it is uploaded under.</summary>
public sealed record LocalImage(string Path, string UploadName);

/// <summary>What the file holds, read before anything goes to the wiki.</summary>
/// <param name="Title">The file's title, its first heading, or its name.</param>
/// <param name="Missing">Targets that name a file that is not there; their links stay as they are.</param>
public sealed record ImportPlan(
    string MarkdownPath,
    string Title,
    string Content,
    IReadOnlyList<ImageReference> References,
    IReadOnlyDictionary<ImageReference, LocalImage> Images,
    IReadOnlyList<string> Missing);

/// <param name="Created">A new page; otherwise an existing one was updated.</param>
public sealed record ImportResult(string PageUrl, bool Created, int Uploaded, IReadOnlyList<string> Missing);

/// <summary>
/// Sends a markdown file and the images it links to into Wiki.js 2 (ADR-0020). The images go
/// into an asset folder that mirrors the page's path without its locale (the path rule): page
/// <c>/en/internal/page</c>, images <c>/internal/page/image.png</c>. Links are rewritten to the
/// names the server reports after the upload, never to guessed ones.
/// </summary>
public static class WikiImporter
{
    /// <summary>Reads the file and finds its images; nothing goes over the network.</summary>
    /// <exception cref="WikiException">The file does not read as text.</exception>
    public static ImportPlan Prepare(string markdownPath, bool removeFirstHeading, Encoding ansi)
    {
        string text = TextDecoder.Decode(File.ReadAllBytes(markdownPath), ansi)
            ?? throw new WikiException(Strings.ToolNotText(markdownPath));
        (string? title, string content) = MarkdownImages.TakeTitle(text, removeFirstHeading);
        string folder = Path.GetDirectoryName(Path.GetFullPath(markdownPath)) ?? "";

        IReadOnlyList<ImageReference> references = MarkdownImages.Find(content);
        var local = new Dictionary<ImageReference, string>();
        var missing = new List<string>();
        foreach (ImageReference reference in references)
        {
            if (MarkdownImages.LocalPath(reference.Target, folder) is not { } path)
            {
                continue;
            }

            if (File.Exists(path))
            {
                local[reference] = path;
            }
            else if (!missing.Contains(reference.Target))
            {
                missing.Add(reference.Target);
            }
        }

        // One file linked from several places is uploaded once.
        IReadOnlyDictionary<string, string> names = AssetNames.ForUpload(local.Values.Distinct(StringComparer.OrdinalIgnoreCase));
        var images = local.ToDictionary(pair => pair.Key, pair => new LocalImage(pair.Value, names[pair.Value]));
        return new ImportPlan(
            markdownPath,
            title ?? Path.GetFileNameWithoutExtension(markdownPath),
            content,
            references,
            images,
            missing);
    }

    /// <param name="existing">
    /// The page to update, which the user agreed to; null to create one. It is written at its own
    /// path, which may differ in case from <paramref name="address"/>: another path would move it.
    /// </param>
    /// <exception cref="WikiException">The wiki refused something; the message says what.</exception>
    public static async Task<ImportResult> RunAsync(
        ImportPlan plan,
        WikiPageAddress address,
        string locale,
        WikiPage? existing,
        LinkStyle linkStyle,
        WikiClient client,
        IToolReport report,
        CancellationToken cancellationToken)
    {
        if (existing is not null)
        {
            address = address with { Path = existing.Path };
            locale = existing.Locale;
            if (!existing.IsMarkdown)
            {
                throw new WikiException(Strings.WikiPageNotMarkdown(address.PageUrl(locale), existing.ContentType));
            }
        }

        LocalImage[] uploads = [.. plan.Images.Values.DistinctBy(image => image.UploadName, StringComparer.OrdinalIgnoreCase)];
        report.Output(Strings.WikiFoundImages(uploads.Length, plan.Missing.Count));
        foreach (string missing in plan.Missing)
        {
            report.Output(Strings.WikiImageMissing(missing));
        }

        var links = new Dictionary<ImageReference, string>();
        if (uploads.Length > 0)
        {
            var created = new List<string>();
            int folder = await client.EnsureFolderAsync(address.FolderSlugs, created, cancellationToken).ConfigureAwait(false);
            report.Output(created.Count > 0
                ? Strings.WikiFolderCreated(address.AssetLink("", fullUrl: false).TrimEnd('/'), string.Join(", ", created))
                : Strings.WikiFolderFound(address.AssetLink("", fullUrl: false).TrimEnd('/')));

            // An upload replaces a file of the same name, and files have no history. Sending a
            // page again replaces its own images, which is the point; a new page's folder may hold
            // someone else's files, so its images take free names instead (ADR-0021).
            var before = new HashSet<string>(await client.AssetNamesAsync(folder, cancellationToken).ConfigureAwait(false), StringComparer.OrdinalIgnoreCase);
            var taken = new HashSet<string>(before, StringComparer.OrdinalIgnoreCase);
            taken.UnionWith(uploads.Select(image => image.UploadName).Where(name => !before.Contains(name)));
            Dictionary<string, string> names = uploads.ToDictionary(
                image => image.UploadName,
                image => existing is null && before.Contains(image.UploadName) ? AssetNames.Free(image.UploadName, taken) : image.UploadName,
                StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < uploads.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LocalImage image = uploads[index];
                string name = names[image.UploadName];
                byte[] bytes = await File.ReadAllBytesAsync(image.Path, cancellationToken).ConfigureAwait(false);
                await client.UploadAsync(folder, name, bytes, cancellationToken).ConfigureAwait(false);
                report.Output(before.Contains(name)
                    ? Strings.WikiUploadedReplacing(Path.GetFileName(image.Path), name)
                    : Strings.WikiUploaded(Path.GetFileName(image.Path), name));
                report.Progress(index + 1, uploads.Length);
            }

            // "ok" from the upload proves nothing: the folder's list says what the server kept.
            IReadOnlyList<string> stored = await client.AssetNamesAsync(folder, cancellationToken).ConfigureAwait(false);
            var stamped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (LocalImage image in uploads)
            {
                string name = names[image.UploadName];
                stamped[image.UploadName] = stored.FirstOrDefault(file => string.Equals(file, name, StringComparison.OrdinalIgnoreCase))
                    ?? throw new WikiException(Strings.WikiUploadNotStored(name));
            }

            foreach ((ImageReference reference, LocalImage image) in plan.Images)
            {
                links[reference] = address.AssetLink(stamped[image.UploadName], linkStyle == LinkStyle.FullUrl);
            }
        }

        string content = MarkdownImages.Rewrite(plan.Content, links);
        var page = new PageContent(
            address.Path,
            locale,
            plan.Title,
            content,
            existing?.Description ?? "",
            existing?.IsPublished ?? true,
            existing?.Tags ?? [],
            existing is null ? null : await client.PageScriptsAsync(existing.Id, cancellationToken).ConfigureAwait(false));

        // A lost update is told by the page's time of saving, read just before it: the read at the
        // start may be minutes old, and Wiki.js changes that time on its own too (a render, a link
        // from a page made elsewhere). Whatever saves the page between here and the read-back is
        // still taken for this update.
        string savedBefore = existing is null ? ""
            : await client.FindPageAsync(address.Path, locale, cancellationToken).ConfigureAwait(false) is { } fresh && fresh.Id == existing.Id
                ? fresh.UpdatedAt
                : existing.UpdatedAt;

        string url = address.PageUrl(locale);
        try
        {
            if (existing is null)
            {
                await client.CreatePageAsync(page, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.UpdatePageAsync(existing.Id, page, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (WikiNoAnswerException lost)
        {
            // Wiki.js saves the page first and answers only after rendering it, indexing it and
            // handing it to storage (a new page also rebuilds the tree); a proxy in front of it may
            // stop waiting before that (G-0115). Whether it was saved is read back, never assumed.
            bool? saved;
            try
            {
                saved = await SavedAsync(client, address.Path, locale, existing, savedBefore, cancellationToken).ConfigureAwait(false);
            }
            catch (WikiException)
            {
                saved = null;
            }

            if (saved == false)
            {
                throw;
            }

            if (saved is null)
            {
                throw new WikiException(Strings.WikiSaveUnknown(lost.Reason, url));
            }

            report.Output(Strings.WikiSavedWithoutAnswer(lost.Reason));
        }

        report.Output(existing is null ? Strings.WikiPageCreated(url) : Strings.WikiPageUpdated(url));
        return new ImportResult(url, existing is null, uploads.Length, plan.Missing);
    }

    /// <summary>
    /// Whether a page whose answer was lost is saved: a new one is there now, an updated one has
    /// another time of saving than <paramref name="before"/>. Null when that cannot be told (no
    /// time was read before).
    /// </summary>
    private static async Task<bool?> SavedAsync(WikiClient client, string path, string locale, WikiPage? existing, string before, CancellationToken cancellationToken)
    {
        if (existing is not null && before.Length == 0)
        {
            return null;
        }

        WikiPage? now = await client.FindPageAsync(path, locale, cancellationToken).ConfigureAwait(false);
        return existing is null
            ? now is not null
            : now is not null && now.Id == existing.Id && now.UpdatedAt.Length > 0 && now.UpdatedAt != before;
    }
}
