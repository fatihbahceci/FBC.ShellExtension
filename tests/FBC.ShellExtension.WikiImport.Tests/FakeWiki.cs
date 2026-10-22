using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace FBC.ShellExtension.WikiImport.Tests;

/// <summary>
/// A Wiki.js 2 in memory, answering the requests the client makes the way the real server does
/// (v2.5): folders found by slug and made without returning an id, slugs lowered the way
/// JavaScript lowers them, folder lists filtered by the key's page rule, uploads that answer
/// "ok" and lower-case the name, mutations that report failure in responseResult.
/// </summary>
internal sealed class FakeWiki : HttpMessageHandler
{
    public List<(int Id, int Parent, string Slug)> Folders { get; } = [];

    public List<(int Folder, string Name, string ContentType, byte[] Bytes)> Assets { get; } = [];

    public List<(int Id, string Path, string Locale, string Title, string Content, string Description, bool IsPublished, string[] Tags)> Pages { get; } = [];

    public List<string> Queries { get; } = [];

    public List<JsonObject> Variables { get; } = [];

    public string? LastAuthorization { get; private set; }

    /// <summary>Uploads answer "ok" but are not kept, as when the server fails after answering.</summary>
    public bool DropUploads { get; set; }

    /// <summary>Every GraphQL request fails with this message, as a revoked key does (HTTP 500).</summary>
    public string? FailWith { get; set; }

    public string? LastUploadDisposition { get; private set; }

    /// <summary>
    /// The key's one page rule, "Path starts with": a folder or file whose path it does not cover
    /// is left out of a list, as checkAccess does. Null for a key that reads everything.
    /// </summary>
    public string? AssetRule { get; set; }

    /// <summary>The content type of a page by its id; "markdown" when not set.</summary>
    public Dictionary<int, string> ContentTypes { get; } = [];

    /// <summary>A page's CSS and JavaScript by its id, which pages.single gives only to a key with manage:pages.</summary>
    public Dictionary<int, (string Css, string Js)> Scripts { get; } = [];

    public bool CanManagePages { get; set; }

    /// <summary>The CSS and JavaScript the last update sent.</summary>
    public (string? Css, string? Js) UpdatedScripts { get; private set; }

    /// <summary>Every request is answered with a redirect here.</summary>
    public string? RedirectTo { get; set; }

    /// <summary>
    /// A GraphQL request whose query holds this gets HTTP 504 from a proxy in front of the wiki,
    /// after the wiki carried it out unless <see cref="TimeOutBeforeTheWiki"/>.
    /// </summary>
    public string? GatewayTimeoutOn { get; set; }

    /// <summary>The proxy's 504 comes before the wiki got the request, which is then not carried out.</summary>
    public bool TimeOutBeforeTheWiki { get; set; }

    /// <summary>The answer is lost to a broken connection instead of the proxy's 504.</summary>
    public bool DropConnection { get; set; }

    /// <summary>After the lost answer, every request gets HTTP 502: the wiki is down from then on.</summary>
    public bool DownAfterTimeout { get; set; }

    /// <summary>The page list leaves out updatedAt.</summary>
    public bool NoUpdatedAt { get; set; }

    /// <summary>When each page was last saved, by its id; a page added by a test has a fixed time.</summary>
    public Dictionary<int, string> UpdatedAt { get; } = [];

    private int _saves;
    private bool _down;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastAuthorization = request.Headers.Authorization?.ToString();
        if (RedirectTo is not null)
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.MovedPermanently);
            redirect.Headers.Location = new Uri(RedirectTo);
            return redirect;
        }

        return request.RequestUri!.AbsolutePath switch
        {
            "/graphql" => GraphQl(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!.AsObject()),
            "/u" => await Upload((MultipartFormDataContent)request.Content!),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        };
    }

    private HttpResponseMessage GraphQl(JsonObject body)
    {
        string query = body["query"]!.GetValue<string>();
        JsonObject variables = body["variables"]?.AsObject() ?? [];
        Queries.Add(query);
        Variables.Add(variables);
        if (FailWith is not null)
        {
            return Json(new JsonObject { ["data"] = new JsonObject(), ["errors"] = new JsonArray(new JsonObject { ["message"] = FailWith }) }, HttpStatusCode.InternalServerError);
        }

        if (_down)
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway) { ReasonPhrase = "Bad Gateway", Content = new StringContent("<html>502 Bad Gateway</html>", Encoding.UTF8, "text/html") };
        }

        bool timeOut = GatewayTimeoutOn is not null && query.Contains(GatewayTimeoutOn, StringComparison.Ordinal);
        if (timeOut && TimeOutBeforeTheWiki)
        {
            return LoseTheAnswer();
        }

        JsonNode data = query switch
        {
            _ when query.Contains("localization") => new JsonObject { ["localization"] = new JsonObject { ["config"] = new JsonObject { ["locale"] = "en" } } },
            _ when query.Contains("profile") => new JsonObject { ["users"] = new JsonObject { ["profile"] = new JsonObject { ["id"] = 1 } } },
            _ when query.Contains("list(locale") => new JsonObject { ["pages"] = new JsonObject { ["list"] = new JsonArray([.. Pages
                .Where(page => page.Locale == variables["locale"]!.GetValue<string>())
                .Select(page => (JsonNode)new JsonObject
                {
                    ["id"] = page.Id, ["path"] = page.Path, ["locale"] = page.Locale, ["title"] = page.Title,
                    ["description"] = page.Description, ["isPublished"] = page.IsPublished,
                    ["tags"] = new JsonArray([.. page.Tags.Select(tag => (JsonNode)JsonValue.Create(tag)!)]),
                    ["contentType"] = ContentTypes.GetValueOrDefault(page.Id, "markdown"),
                    ["updatedAt"] = NoUpdatedAt ? null : UpdatedAt.GetValueOrDefault(page.Id, "2026-01-01T00:00:00.000Z"),
                })]) } },
            _ when query.Contains("single(") => Single(variables["id"]!.GetValue<int>()),
            _ when query.Contains("folders(") => new JsonObject { ["assets"] = new JsonObject { ["folders"] = new JsonArray([.. Folders
                .Where(folder => folder.Parent == variables["parent"]!.GetValue<int>() && Readable(FolderPath(folder.Id)))
                .Select(folder => (JsonNode)new JsonObject { ["id"] = folder.Id, ["slug"] = folder.Slug })]) } },
            _ when query.Contains("createFolder") => CreateFolder(variables),
            _ when query.Contains("list(folderId") => new JsonObject { ["assets"] = new JsonObject { ["list"] = new JsonArray([.. Assets
                .Where(asset => asset.Folder == variables["folder"]!.GetValue<int>())
                .Select(asset => (JsonNode)new JsonObject { ["filename"] = asset.Name })]) } },
            _ when query.Contains("create(content") => CreatePage(variables),
            _ when query.Contains("update(id") => UpdatePage(variables),
            _ => throw new InvalidOperationException("Unexpected query: " + query),
        };

        if (timeOut)
        {
            return LoseTheAnswer();
        }

        return data is JsonObject { Count: 1 } error && error.ContainsKey("errors")
            ? Json(new JsonObject { ["data"] = new JsonObject(), ["errors"] = error["errors"]!.DeepClone() })
            : Json(new JsonObject { ["data"] = data });
    }

    /// <summary>checkAccess for a START rule: '/' + path starts with '/' + rule.</summary>
    private bool Readable(string path) => AssetRule is null || ("/" + path).StartsWith("/" + AssetRule, StringComparison.Ordinal);

    private string FolderPath(int id)
    {
        var slugs = new List<string>();
        for (int current = id; current != 0;)
        {
            var folder = Folders.Single(item => item.Id == current);
            slugs.Insert(0, folder.Slug);
            current = folder.Parent;
        }

        return string.Join('/', slugs);
    }

    private JsonNode Single(int id) => CanManagePages
        ? new JsonObject { ["pages"] = new JsonObject { ["single"] = new JsonObject
        {
            ["scriptCss"] = Scripts.GetValueOrDefault(id).Css ?? "",
            ["scriptJs"] = Scripts.GetValueOrDefault(id).Js ?? "",
        } } }
        : new JsonObject { ["errors"] = new JsonArray(new JsonObject { ["message"] = "You are not authorized to view this page." }) };

    private JsonNode CreateFolder(JsonObject variables)
    {
        // The real one stores sanitize(slug).toLowerCase(), answers 2002 for a folder that is
        // there, and returns no id. JavaScript lowers İ to i and a combining dot.
        int parent = variables["parent"]!.GetValue<int>();
        string slug = variables["slug"]!.GetValue<string>().Replace("\u0130", "i\u0307", StringComparison.Ordinal).ToLowerInvariant();
        if (Folders.Any(folder => folder.Parent == parent && folder.Slug == slug))
        {
            return new JsonObject { ["assets"] = new JsonObject { ["createFolder"] = new JsonObject { ["responseResult"] = Status(false, 2002, "Asset folder already exists.") } } };
        }

        Folders.Add((Folders.Count + 1, parent, slug));
        return new JsonObject { ["assets"] = new JsonObject { ["createFolder"] = new JsonObject { ["responseResult"] = Status(true) } } };
    }

    private JsonNode CreatePage(JsonObject variables)
    {
        string path = variables["path"]!.GetValue<string>();
        if (path.Contains('.'))
        {
            return new JsonObject { ["pages"] = new JsonObject { ["create"] = new JsonObject { ["responseResult"] = Status(false, 6005, "Invalid Path"), ["page"] = null } } };
        }

        int id = Pages.Count + 1;
        Pages.Add((id, path, variables["locale"]!.GetValue<string>(), variables["title"]!.GetValue<string>(), variables["content"]!.GetValue<string>(),
            variables["description"]!.GetValue<string>(), variables["isPublished"]!.GetValue<bool>(), [.. variables["tags"]!.AsArray().Select(tag => tag!.GetValue<string>())]));
        UpdatedAt[id] = Now();
        return new JsonObject { ["pages"] = new JsonObject { ["create"] = new JsonObject { ["responseResult"] = Status(true), ["page"] = new JsonObject { ["id"] = id } } } };
    }

    private JsonNode UpdatePage(JsonObject variables)
    {
        int id = variables["id"]!.GetValue<int>();
        UpdatedScripts = (variables["scriptCss"]?.GetValue<string>(), variables["scriptJs"]?.GetValue<string>());
        int index = Pages.FindIndex(page => page.Id == id);
        Pages[index] = (id, variables["path"]!.GetValue<string>(), variables["locale"]!.GetValue<string>(), variables["title"]!.GetValue<string>(),
            variables["content"]!.GetValue<string>(), variables["description"]!.GetValue<string>(), variables["isPublished"]!.GetValue<bool>(),
            [.. variables["tags"]!.AsArray().Select(tag => tag!.GetValue<string>())]);
        UpdatedAt[id] = Now();
        return new JsonObject { ["pages"] = new JsonObject { ["update"] = new JsonObject { ["responseResult"] = Status(true) } } };
    }

    private async Task<HttpResponseMessage> Upload(MultipartFormDataContent form)
    {
        HttpContent[] parts = [.. form];
        if (parts.Length != 2 || parts.Any(part => part.Headers.ContentDisposition?.Name?.Trim('"') != "mediaUpload"))
        {
            return Json(new JsonObject { ["succeeded"] = false, ["message"] = "Missing upload payload." }, HttpStatusCode.BadRequest);
        }

        int folder = JsonNode.Parse(await parts[0].ReadAsStringAsync())!["folderId"]!.GetValue<int>();
        LastUploadDisposition = parts[1].Headers.ContentDisposition!.ToString();
        string name = parts[1].Headers.ContentDisposition!.FileName!.Trim('"').ToLowerInvariant().Replace(' ', '_');
        if (!DropUploads)
        {
            Assets.RemoveAll(asset => asset.Folder == folder && asset.Name == name);
            Assets.Add((folder, name, parts[1].Headers.ContentType!.MediaType!, await parts[1].ReadAsByteArrayAsync()));
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
    }

    /// <summary>As the wiki writes it: $beforeInsert and $beforeUpdate set new Date().toISOString().</summary>
    private string Now() => $"2026-10-22T12:00:{++_saves:00}.000Z";

    private HttpResponseMessage LoseTheAnswer()
    {
        _down = DownAfterTimeout;
        return DropConnection ? throw new HttpRequestException("The response ended prematurely.") : GatewayTimeout();
    }

    /// <summary>What nginx answers when the wiki behind it takes longer than proxy_read_timeout.</summary>
    private static HttpResponseMessage GatewayTimeout() => new(HttpStatusCode.GatewayTimeout)
    {
        ReasonPhrase = "Gateway Time-out",
        Content = new StringContent("<html><head><title>504 Gateway Time-out</title></head><body><center><h1>504 Gateway Time-out</h1></center></body></html>", Encoding.UTF8, "text/html"),
    };

    private static JsonObject Status(bool succeeded, int code = 0, string? message = null) => new()
    {
        ["succeeded"] = succeeded,
        ["errorCode"] = code,
        ["slug"] = succeeded ? "ok" : "PageIllegalPath",
        ["message"] = message,
    };

    private static HttpResponseMessage Json(JsonNode body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
}
