using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.WikiImport;

/// <summary>A failure the user can act on; the message says what happened in their terms.</summary>
public class WikiException(string message) : Exception(message);

/// <summary>The server did not take the API key: unknown, revoked, expired, or the API is off.</summary>
public sealed class WikiKeyException(string message) : WikiException(message);

/// <summary>
/// No answer came: the connection broke, the client's own wait ran out, or a server in front of
/// the wiki answered for it with an error page (HTTP 5xx, 504 when it stopped waiting). The
/// request may have been carried out all the same (G-0115).
/// </summary>
/// <param name="reason">What came instead of an answer, such as "HTTP 504 Gateway Time-out".</param>
public sealed class WikiNoAnswerException(string message, string reason) : WikiException(message)
{
    public string Reason { get; } = reason;
}

/// <param name="DefaultLocale">The site's locale, for an address that names none.</param>
public sealed record WikiSite(string DefaultLocale);

/// <param name="ContentType">"markdown" for a page of the markdown editor; "html" for the visual and the raw HTML editors.</param>
/// <param name="UpdatedAt">When the page was last saved, as the wiki writes it; "" when not known.</param>
public sealed record WikiPage(int Id, string Path, string Locale, string Title, string Description, bool IsPublished, IReadOnlyList<string> Tags, string ContentType = "markdown", string UpdatedAt = "")
{
    /// <summary>An update keeps a page's editor, so only a markdown page can take this file's text.</summary>
    public bool IsMarkdown => string.Equals(ContentType, "markdown", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A page's own CSS and JavaScript, which an update would otherwise clear.</summary>
public sealed record PageScripts(string Css, string Js);

/// <param name="Description">Kept as it is on an update; Wiki.js has no "leave unchanged".</param>
/// <param name="IsPublished">Likewise: an update without it would unpublish the page.</param>
/// <param name="Scripts">Likewise for a key allowed to write them; null when the key cannot read them.</param>
public sealed record PageContent(string Path, string Locale, string Title, string Content, string Description, bool IsPublished, IReadOnlyList<string> Tags, PageScripts? Scripts = null);

/// <summary>
/// Wiki.js 2's GraphQL API and its upload endpoint, with an API key (ADR-0020). The queries and
/// their quirks are those of 2.5 (requarks/wiki, tag v2.5.315): a missing page is looked up
/// through the page list, which needs only read:pages; a new folder's id has to be read back;
/// an upload answers "ok" even when it failed, so the caller reads the folder's list after it.
/// The key goes in a header and is never written anywhere else.
/// </summary>
public sealed class WikiClient : IDisposable
{
    private const string ResponseResult = "responseResult { succeeded errorCode slug message }";

    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public WikiClient(string baseUrl, string apiKey, HttpMessageHandler? handler = null)
    {
        _baseUrl = baseUrl;

        // A redirect would drop the key and turn the POST into a GET; it is reported instead.
        _http = new HttpClient(handler ?? new SocketsHttpHandler { AllowAutoRedirect = false });
        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.Timeout = TimeSpan.FromMinutes(5);
        // A key or token pasted with a space or a line break around it is still the same one.
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
    }

    public void Dispose() => _http.Dispose();

    /// <summary>The site's default locale; also proves this is a Wiki.js 2 GraphQL endpoint.</summary>
    public async Task<WikiSite> GetSiteAsync(CancellationToken cancellationToken)
    {
        JsonNode data = await QueryAsync("{ localization { config { locale } } }", new JsonObject(), cancellationToken).ConfigureAwait(false);
        string? locale = data["localization"]?["config"]?["locale"]?.GetValue<string>();
        return new WikiSite(string.IsNullOrEmpty(locale) ? "en" : locale);
    }

    /// <summary>
    /// An expired key or one with a bad signature is not refused: the request quietly runs as a
    /// guest. Asking for the profile tells the two apart, since a guest has none.
    /// </summary>
    /// <exception cref="WikiKeyException">The key was not taken.</exception>
    public async Task CheckKeyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await QueryAsync("{ users { profile { id } } }", new JsonObject(), cancellationToken).ConfigureAwait(false);
        }
        catch (WikiException ex) when (ex is not WikiKeyException and not WikiNoAnswerException)
        {
            throw new WikiKeyException(Strings.WikiKeyRejected(ex.Message));
        }
    }

    /// <summary>The page at <paramref name="path"/> in <paramref name="locale"/>, or null.</summary>
    public async Task<WikiPage?> FindPageAsync(string path, string locale, CancellationToken cancellationToken)
    {
        JsonNode data = await QueryAsync(
            "query ($locale: String) { pages { list(locale: $locale) { id path locale title description isPublished tags contentType updatedAt } } }",
            new JsonObject { ["locale"] = locale },
            cancellationToken).ConfigureAwait(false);

        // A database that compares paths without case (MySQL, SQL Server) holds one page for both
        // spellings, so the other spelling is the page too; the caller writes it at its own path.
        JsonNode? match = data["pages"]?["list"]?.AsArray()
            .FirstOrDefault(page => string.Equals(page?["path"]?.GetValue<string>(), path, StringComparison.Ordinal))
            ?? data["pages"]?["list"]?.AsArray()
                .FirstOrDefault(page => string.Equals(page?["path"]?.GetValue<string>(), path, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return null;
        }

        return new WikiPage(
            match["id"]!.GetValue<int>(),
            match["path"]!.GetValue<string>(),
            match["locale"]?.GetValue<string>() ?? locale,
            match["title"]?.GetValue<string>() ?? "",
            match["description"]?.GetValue<string>() ?? "",
            match["isPublished"]?.GetValue<bool>() ?? true,
            [.. (match["tags"]?.AsArray() ?? []).Select(tag => tag?.GetValue<string>()).OfType<string>()],
            match["contentType"]?.GetValue<string>() ?? "markdown",
            match["updatedAt"]?.ToString() ?? "");
    }

    /// <summary>
    /// The page's CSS and JavaScript, so that an update can send them back. Reading them needs
    /// manage:pages or delete:pages; without either the answer is null. (The page's publishing
    /// dates are not sent back: KI-0004.) A lost answer is not taken for "cannot read": the
    /// update would then clear them.
    /// </summary>
    public async Task<PageScripts?> PageScriptsAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            JsonNode data = await QueryAsync(
                "query ($id: Int!) { pages { single(id: $id) { scriptCss scriptJs } } }",
                new JsonObject { ["id"] = id },
                cancellationToken).ConfigureAwait(false);
            return data["pages"]?["single"] is { } page
                ? new PageScripts(page["scriptCss"]?.GetValue<string>() ?? "", page["scriptJs"]?.GetValue<string>() ?? "")
                : null;
        }
        catch (WikiException ex) when (ex is not WikiKeyException and not WikiNoAnswerException)
        {
            return null;
        }
    }

    /// <summary>The id of the folder the slugs lead to from the root (0), made where missing.</summary>
    /// <param name="created">Gets the slugs of the folders that had to be made.</param>
    public async Task<int> EnsureFolderAsync(IReadOnlyList<string> slugs, List<string> created, CancellationToken cancellationToken)
    {
        int parent = 0;
        for (int level = 0; level < slugs.Count; level++)
        {
            string slug = slugs[level];
            int? id = await FindFolderAsync(parent, slug, cancellationToken).ConfigureAwait(false);
            if (id is null)
            {
                JsonNode data = await QueryAsync(
                    $"mutation ($parent: Int!, $slug: String!) {{ assets {{ createFolder(parentFolderId: $parent, slug: $slug) {{ {ResponseResult} }} }} }}",
                    new JsonObject { ["parent"] = parent, ["slug"] = slug },
                    cancellationToken).ConfigureAwait(false);

                // 2002: made a moment ago by someone else; it is there either way.
                CheckResult(data["assets"]?["createFolder"]?["responseResult"], allowedCode: 2002);
                created.Add(slug);

                // The answer carries no id: the folder is looked up again. The list leaves out a
                // folder no page rule lets the key read, judged by the folder's own path: a rule
                // "starts with internal/" hides the folder "internal" (ADR-0021).
                id = await FindFolderAsync(parent, slug, cancellationToken).ConfigureAwait(false)
                    ?? throw new WikiException(Strings.WikiFolderHidden(string.Join('/', slugs.Take(level + 1))));
            }

            parent = id.Value;
        }

        return parent;
    }

    /// <summary>The names of the files in the folder, as the server stored them.</summary>
    public async Task<IReadOnlyList<string>> AssetNamesAsync(int folderId, CancellationToken cancellationToken)
    {
        JsonNode data = await QueryAsync(
            "query ($folder: Int!) { assets { list(folderId: $folder, kind: ALL) { filename } } }",
            new JsonObject { ["folder"] = folderId },
            cancellationToken).ConfigureAwait(false);
        return [.. (data["assets"]?["list"]?.AsArray() ?? []).Select(asset => asset?["filename"]?.GetValue<string>()).OfType<string>()];
    }

    /// <summary>
    /// POST /u: two parts named "mediaUpload", the folder as JSON and then the file. The name is
    /// written plainly (no RFC 5987 <c>filename*</c>, which the server's parser decodes wrongly).
    /// </summary>
    public async Task UploadAsync(int folderId, string fileName, byte[] content, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var folder = new StringContent($"{{\"folderId\":{folderId}}}", Encoding.UTF8);
        folder.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"mediaUpload\"" };
        form.Add(folder);

        var file = new ByteArrayContent(content);
        file.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"mediaUpload\"", FileName = $"\"{fileName}\"" };
        file.Headers.ContentType = new MediaTypeHeaderValue(AssetNames.ContentType(fileName));
        form.Add(file);

        HttpResponseMessage response = await SendAsync(() => _http.PostAsync("u", form, cancellationToken)).ConfigureAwait(false);
        using (response)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string message = MessageOf(body) ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            throw response.StatusCode == HttpStatusCode.Forbidden
                ? new WikiException(Strings.WikiUploadForbidden(fileName, message))
                : new WikiException(Strings.WikiUploadFailed(fileName, message));
        }
    }

    /// <returns>The new page's id.</returns>
    public async Task<int> CreatePageAsync(PageContent page, CancellationToken cancellationToken)
    {
        JsonNode data = await QueryAsync(
            $$"""
            mutation ($content: String!, $description: String!, $editor: String!, $isPublished: Boolean!, $isPrivate: Boolean!, $locale: String!, $path: String!, $tags: [String]!, $title: String!) {
              pages { create(content: $content, description: $description, editor: $editor, isPublished: $isPublished, isPrivate: $isPrivate, locale: $locale, path: $path, tags: $tags, title: $title) {
                {{ResponseResult}} page { id } } }
            }
            """,
            PageVariables(page),
            cancellationToken).ConfigureAwait(false);
        JsonNode? result = data["pages"]?["create"];
        CheckResult(result?["responseResult"]);
        return result?["page"]?["id"]?.GetValue<int>() ?? 0;
    }

    /// <summary>
    /// Every field is sent: one left out is not kept but cleared (isPublished, tags, and the CSS
    /// and JavaScript when the key may write them). A path or locale other than the page's own
    /// moves the page.
    /// </summary>
    public async Task UpdatePageAsync(int id, PageContent page, CancellationToken cancellationToken)
    {
        JsonObject variables = PageVariables(page);
        variables["id"] = id;
        variables["scriptCss"] = page.Scripts?.Css;
        variables["scriptJs"] = page.Scripts?.Js;
        JsonNode data = await QueryAsync(
            $$"""
            mutation ($id: Int!, $content: String!, $description: String!, $editor: String!, $isPublished: Boolean!, $isPrivate: Boolean!, $locale: String!, $path: String!, $tags: [String]!, $title: String!, $scriptCss: String, $scriptJs: String) {
              pages { update(id: $id, content: $content, description: $description, editor: $editor, isPublished: $isPublished, isPrivate: $isPrivate, locale: $locale, path: $path, tags: $tags, title: $title, scriptCss: $scriptCss, scriptJs: $scriptJs) {
                {{ResponseResult}} } }
            }
            """,
            variables,
            cancellationToken).ConfigureAwait(false);
        CheckResult(data["pages"]?["update"]?["responseResult"]);
    }

    private static JsonObject PageVariables(PageContent page) => new()
    {
        ["content"] = page.Content,
        ["description"] = page.Description,
        ["editor"] = "markdown",
        ["isPublished"] = page.IsPublished,

        // Private pages are not finished in Wiki.js 2; its own editor always sends false.
        ["isPrivate"] = false,
        ["locale"] = page.Locale,
        ["path"] = page.Path,
        ["tags"] = new JsonArray([.. page.Tags.Select(tag => (JsonNode?)JsonValue.Create(tag))]),
        ["title"] = page.Title,
    };

    private async Task<int?> FindFolderAsync(int parent, string slug, CancellationToken cancellationToken)
    {
        JsonNode data = await QueryAsync(
            "query ($parent: Int!) { assets { folders(parentFolderId: $parent) { id slug } } }",
            new JsonObject { ["parent"] = parent },
            cancellationToken).ConfigureAwait(false);
        JsonNode? folder = data["assets"]?["folders"]?.AsArray()
            .FirstOrDefault(item => string.Equals(item?["slug"]?.GetValue<string>(), slug, StringComparison.OrdinalIgnoreCase));
        return folder?["id"]?.GetValue<int>();
    }

    /// <summary>A mutation reports its failure in responseResult rather than as a GraphQL error.</summary>
    private static void CheckResult(JsonNode? result, int? allowedCode = null)
    {
        if (result is null)
        {
            throw new WikiException(Strings.WikiNoAnswer());
        }

        if (result["succeeded"]?.GetValue<bool>() == true || (allowedCode is { } code && result["errorCode"]?.GetValue<int>() == code))
        {
            return;
        }

        string message = result["message"]?.GetValue<string>() ?? result["slug"]?.GetValue<string>() ?? "?";
        throw new WikiException(Strings.WikiRefused(message, result["errorCode"]?.GetValue<int>() ?? 0));
    }

    /// <summary>Sends the query with its variables (never pasted into the text) and returns "data".</summary>
    private async Task<JsonNode> QueryAsync(string query, JsonObject variables, CancellationToken cancellationToken)
    {
        string body = new JsonObject { ["query"] = query, ["variables"] = variables }.ToJsonString();
        using HttpResponseMessage response = await SendAsync(() =>
            _http.PostAsync("graphql", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken)).ConfigureAwait(false);
        string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        JsonObject? json;
        try
        {
            json = JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            json = null;
        }

        // A revoked key, or the API switched off, comes back as an error with HTTP 500. Only the
        // first is the key's fault ("API Key is invalid or was revoked."); "API is disabled" is not.
        if (json?["errors"] is JsonArray { Count: > 0 } errors)
        {
            string message = (errors[0] as JsonObject)?["message"] is JsonValue value && value.TryGetValue(out string? said) ? said : "?";
            throw message.Contains("API Key", StringComparison.OrdinalIgnoreCase) && (message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("revoked", StringComparison.OrdinalIgnoreCase))
                ? new WikiKeyException(Strings.WikiKeyRejected(message))
                : new WikiException(Strings.WikiError(message));
        }

        if (!response.IsSuccessStatusCode || json?["data"] is not { } data)
        {
            string status = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            // An error page instead of the wiki's answer: a server in front of it (a proxy) found
            // the wiki down, or stopped waiting while it was still at work (G-0115).
            if ((int)response.StatusCode >= 500)
            {
                throw new WikiNoAnswerException(Strings.WikiGatewayNoAnswer(_baseUrl, status), status);
            }

            // No GraphQL answer at all: not a Wiki.js 2 at this address (3.x has its API elsewhere).
            throw new WikiException(Strings.WikiNotWikiJs2(_baseUrl, status));
        }

        return data;
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try
        {
            response = await send().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // The request may have gone out before the connection broke.
            throw new WikiNoAnswerException(Strings.WikiUnreachable(_baseUrl, ex.Message), ex.Message);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new WikiNoAnswerException(Strings.WikiUnreachable(_baseUrl, ex.Message), ex.Message);
        }

        if ((int)response.StatusCode is >= 300 and < 400)
        {
            string location = response.Headers.Location?.ToString() ?? "?";
            response.Dispose();
            throw new WikiException(Strings.WikiRedirected(_baseUrl, location));
        }

        return response;
    }

    private static string? MessageOf(string body)
    {
        try
        {
            return JsonNode.Parse(body)?["message"]?.GetValue<string>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
