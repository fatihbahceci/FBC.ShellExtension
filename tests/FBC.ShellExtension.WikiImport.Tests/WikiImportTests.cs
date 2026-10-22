using System.Text;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.WikiImport.Tests;

[TestClass]
public sealed class WikiPageAddressTests
{
    private const string Example = "https://wiki.example.com:19888/en/internal/third-party/about-markdown-typora-wikijs-and-documentation";

    [TestMethod]
    [DataRow("https://wiki.example.com:19888/en/internal/page?x=1", "https://wiki.example.com:19888")]
    [DataRow("  https://Wiki.Example.com/  ", "https://wiki.example.com")]
    [DataRow("http://wiki.local", "http://wiki.local")]
    [DataRow("wiki.example.com", null)]
    [DataRow("ftp://wiki.example.com", null)]
    [DataRow("", null)]
    public void BaseUrlOf_IsTheWikiOfAnyOfItsAddresses(string text, string? expected)
    {
        Assert.AreEqual(expected, WikiPageAddress.BaseUrlOf(text));
    }

    [TestMethod]
    public void Address_GivesTheSiteTheLocaleAndThePath()
    {
        WikiPageAddress address = WikiPageAddress.Parse(Example);

        Assert.AreEqual("https://wiki.example.com:19888", address.BaseUrl);
        Assert.AreEqual("en", address.Locale);
        Assert.AreEqual("internal/third-party/about-markdown-typora-wikijs-and-documentation", address.Path);
        Assert.AreEqual(Example, address.PageUrl("en"));
    }

    [TestMethod]
    public void AssetLinks_FollowThePathWithoutTheLocale()
    {
        WikiPageAddress address = WikiPageAddress.Parse(Example);

        Assert.AreEqual(
            "/internal/third-party/about-markdown-typora-wikijs-and-documentation/image-20210622223543110.png",
            address.AssetLink("image-20210622223543110.png", fullUrl: false));
        Assert.AreEqual(
            "https://wiki.example.com:19888/internal/third-party/about-markdown-typora-wikijs-and-documentation/image-20210622223543110.png",
            address.AssetLink("image-20210622223543110.png", fullUrl: true));
    }

    [TestMethod]
    public void Address_WithoutALocale_LeavesItToTheSite_AndLowersTheFolders()
    {
        WikiPageAddress address = WikiPageAddress.Parse("http://wiki.local/Docs/My%2DPage/?x=1#part");

        Assert.IsNull(address.Locale);
        Assert.AreEqual("Docs/My-Page", address.Path);
        CollectionAssert.AreEqual(new[] { "docs", "my-page" }, address.FolderSlugs.ToArray());
    }

    [TestMethod]
    [DataRow("wiki.example.com/en/page")]
    [DataRow("ftp://wiki.example.com/en/page")]
    [DataRow("https://wiki.example.com/en/")]
    [DataRow("https://wiki.example.com/en/login/page")]
    [DataRow("https://wiki.example.com/en/x/page")]
    [DataRow("https://wiki.example.com/en/tr/page")]
    [DataRow("https://wiki.example.com/en/docs/page.md")]
    [DataRow("https://wiki.example.com/en/docs/my%20page")]
    [DataRow("https://wiki.example.com/en/docs/a%22b")]
    [DataRow("https://wiki.example.com/en/docs/AUX")]
    [DataRow("https://wiki.example.com/en/docs/a%01b")]
    public void Address_ThatWikiJsCannotServe_IsRefusedWithAReason(string text)
    {
        Assert.IsNotNull(WikiPageAddress.Problem(text));
    }

    [TestMethod]
    public void FolderSlugs_AreLoweredTheWayTheServerLowersThem()
    {
        WikiPageAddress address = WikiPageAddress.Parse("https://wiki.example.com/tr/%C4%B0leti%C5%9Fim/Rehber");

        Assert.AreEqual("İletişim/Rehber", address.Path);
        CollectionAssert.AreEqual(new[] { "i\u0307leti\u015Fim", "rehber" }, address.FolderSlugs.ToArray());
    }
}

[TestClass]
public sealed class MarkdownImagesTests
{
    [TestMethod]
    public void Find_TakesMarkdownAndHtmlImages_ButNotCode()
    {
        string markdown = """
            # Title

            ![a](doc.assets/one.png "First") and ![b](<doc.assets/two words.png>)
            <img src="doc.assets/three.png" style="zoom:50%;" alt="c">
            `![not](code.png)`

            ```
            ![not](fenced.png)
            ```
            ![web](https://example.com/x.png)
            """;

        string[] targets = [.. MarkdownImages.Find(markdown).Select(reference => reference.Target)];

        CollectionAssert.AreEqual(new[] { "doc.assets/one.png", "doc.assets/two words.png", "doc.assets/three.png", "https://example.com/x.png" }, targets);
    }

    [TestMethod]
    public void Rewrite_ChangesOnlyTheTargets()
    {
        string markdown = """![a](doc.assets/one.png "First") <img src="doc.assets/two.png" style="zoom:50%;">""";
        IReadOnlyList<ImageReference> found = MarkdownImages.Find(markdown);

        string rewritten = MarkdownImages.Rewrite(markdown, new Dictionary<ImageReference, string>
        {
            [found[0]] = "/docs/page/one.png",
            [found[1]] = "/docs/page/two.png",
        });

        Assert.AreEqual("""![a](/docs/page/one.png "First") <img src="/docs/page/two.png" style="zoom:50%;">""", rewritten);
    }

    [TestMethod]
    public void LocalPath_DecodesTyporasEscapes_AndSkipsTheWeb()
    {
        string folder = Path.Combine(Path.GetTempPath(), "docs");

        Assert.AreEqual(Path.Combine(folder, "a.assets", "image one.png"), MarkdownImages.LocalPath("a.assets/image%20one.png", folder));
        Assert.IsNull(MarkdownImages.LocalPath("https://example.com/x.png", folder));
        Assert.IsNull(MarkdownImages.LocalPath("data:image/png;base64,AAAA", folder));
        Assert.IsNull(MarkdownImages.LocalPath("#anchor", folder));
        Assert.AreEqual(@"C:\Pictures\x.png", MarkdownImages.LocalPath(@"C:\Pictures\x.png", folder));
    }

    [TestMethod]
    public void TakeTitle_UsesTheHeadingTheTextOpensWith_AndRemovesIt_WhenAsked()
    {
        string markdown = "\n# The Title\n\nText\n# Second\n";

        (string? title, string content) = MarkdownImages.TakeTitle(markdown, removeFirstHeading: true);
        (_, string kept) = MarkdownImages.TakeTitle(markdown, removeFirstHeading: false);

        Assert.AreEqual("The Title", title);
        Assert.AreEqual("Text\n# Second\n", content);
        StringAssert.Contains(kept, "# The Title");
    }

    [TestMethod]
    public void TakeTitle_TakesAnUnderlinedHeading_ButNoHeadingFurtherDown()
    {
        (string? underlined, string rest) = MarkdownImages.TakeTitle("The Title\n=====\nText", removeFirstHeading: true);
        Assert.AreEqual(("The Title", "Text"), (underlined, rest));

        string markdown = "Intro\n\n```\n# not a title\n```\n# Appendix\nText";
        (string? title, string content) = MarkdownImages.TakeTitle(markdown, removeFirstHeading: true);
        Assert.IsNull(title);
        Assert.AreEqual(markdown, content);
    }

    [TestMethod]
    public void Find_GoesOnAfterCode_ThatHoldsAnUnclosedImage()
    {
        string markdown = "`![` then ![real](a.png)\n`<img` and\n<img src=\"b.png\">";

        string[] targets = [.. MarkdownImages.Find(markdown).Select(reference => reference.Target)];

        CollectionAssert.AreEqual(new[] { "a.png", "b.png" }, targets);
    }

    [TestMethod]
    public void Find_TakesBracketsInTheAltText_EscapedQuotes_AndReferenceImages()
    {
        string markdown = """
            ![a [b] c](one.png) ![t](two.png "say \"hi\"")
            ![ref][Logo] ![Logo][] ![shot]

            [logo]: three.png "Logo"
            [shot]: <four five.png>
            [link]: not-an-image.png
            """;

        string[] targets = [.. MarkdownImages.Find(markdown).Select(reference => reference.Target)];

        CollectionAssert.AreEqual(new[] { "one.png", "two.png", "three.png", "four five.png" }, targets);
    }

    [TestMethod]
    public void Find_SkipsFencesInQuotesAndListItems()
    {
        string markdown = "> ```\n> ![not](quoted.png)\n> ```\n- item\n\n      ```\n      ![not](listed.png)\n      ```\n![yes](kept.png)";

        string[] targets = [.. MarkdownImages.Find(markdown).Select(reference => reference.Target)];

        CollectionAssert.AreEqual(new[] { "kept.png" }, targets);
    }

    [TestMethod]
    public void TakeTitle_ReadsAndDropsFrontMatter()
    {
        (string? title, string content) = MarkdownImages.TakeTitle("---\ntitle: \"From YAML\"\ntags: a\n---\n# Heading\nBody", removeFirstHeading: true);

        Assert.AreEqual("From YAML", title);
        Assert.AreEqual("Body", content);
    }
}

[TestClass]
public sealed class AssetNamesTests
{
    [TestMethod]
    [DataRow("Ekran Görüntüsü 1.PNG", "ekran_goruntusu_1.png")]
    [DataRow("İçerik;şema#2.jpg", "icerik_sema_2.jpg")]
    [DataRow("image-20210622223543110.png", "image-20210622223543110.png")]
    [DataRow("CON.png", "_con.png")]
    [DataRow("日本.png", "image.png")]
    [DataRow("a..b.png", "a.b.png")]
    public void ForUpload_MakesPlainAsciiInTheServersForm(string name, string expected)
    {
        Assert.AreEqual(expected, AssetNames.ForUpload(name));
    }

    [TestMethod]
    public void ForUpload_KeepsNamesApart()
    {
        IReadOnlyDictionary<string, string> names = AssetNames.ForUpload([
            Path.Combine("a", "Shot.png"), Path.Combine("b", "shot.png"), Path.Combine("a", "Shot.png")]);

        Assert.AreEqual("shot.png", names[Path.Combine("a", "Shot.png")]);
        Assert.AreEqual("shot-2.png", names[Path.Combine("b", "shot.png")]);
        Assert.AreEqual("image/png", AssetNames.ContentType("shot.png"));
        Assert.AreEqual("application/octet-stream", AssetNames.ContentType("notes.txt"));
    }
}

[TestClass]
public sealed class WikiClientTests
{
    [TestMethod]
    public async Task EnsureFolder_FindsAFolderWithAnI_AgainOnTheNextRun()
    {
        var wiki = new FakeWiki();
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        IReadOnlyList<string> slugs = WikiPageAddress.Parse("https://wiki.local/tr/%C4%B0K/rehber").FolderSlugs;

        int first = await client.EnsureFolderAsync(slugs, [], default);
        var created = new List<string>();
        int second = await client.EnsureFolderAsync(slugs, created, default);

        Assert.AreEqual(first, second);
        Assert.IsEmpty(created);
    }

    [TestMethod]
    public async Task AFolderTheKeyCannotSee_SaysWhichRuleIsMissing()
    {
        var hidden = new FakeWiki { AssetRule = "internal/" };
        using var client = new WikiClient("https://wiki.local", "k", hidden);
        WikiException ex = await Assert.ThrowsExactlyAsync<WikiException>(() => client.EnsureFolderAsync(["internal", "notes"], [], default));
        StringAssert.Contains(ex.Message, "Path starts with internal");

        var covered = new FakeWiki { AssetRule = "internal" };
        using var other = new WikiClient("https://wiki.local", "k", covered);
        Assert.AreEqual(2, await other.EnsureFolderAsync(["internal", "notes"], [], default));
    }

    [TestMethod]
    public async Task ARedirect_IsReported_AndTheApiSwitchedOff_IsNoKeyFailure()
    {
        using var redirected = new WikiClient("http://wiki.local", "k", new FakeWiki { RedirectTo = "https://wiki.local/graphql" });
        WikiException ex = await Assert.ThrowsExactlyAsync<WikiException>(() => redirected.GetSiteAsync(default));
        StringAssert.Contains(ex.Message, "https://wiki.local/graphql");

        using var disabled = new WikiClient("https://wiki.local", "k", new FakeWiki { FailWith = "API is disabled. You must enable it from the Administration Area first." });
        await Assert.ThrowsExactlyAsync<WikiException>(() => disabled.GetSiteAsync(default));
    }

    [TestMethod]
    public async Task Queries_CarryTheKeyAndPassValuesAsVariables()
    {
        var wiki = new FakeWiki();
        using var client = new WikiClient("https://wiki.local", "secret-key", wiki);

        await client.CreatePageAsync(new PageContent("docs/page", "en", "T", "\"quoted\" } content", "", true, []), default);

        Assert.AreEqual("Bearer secret-key", wiki.LastAuthorization);
        Assert.IsFalse(wiki.Queries.Single().Contains("quoted", StringComparison.Ordinal));
        Assert.AreEqual("\"quoted\" } content", wiki.Pages.Single().Content);
    }

    [TestMethod]
    public async Task ARevokedKey_IsAKeyFailure_AndARefusedPathSaysWhy()
    {
        var revoked = new FakeWiki { FailWith = "API Key is invalid or was revoked." };
        using var client = new WikiClient("https://wiki.local", "k", revoked);
        await Assert.ThrowsExactlyAsync<WikiKeyException>(() => client.GetSiteAsync(default));

        var wiki = new FakeWiki();
        using var other = new WikiClient("https://wiki.local", "k", wiki);
        WikiException refused = await Assert.ThrowsExactlyAsync<WikiException>(
            () => other.CreatePageAsync(new PageContent("docs/a.b", "en", "T", "c", "", true, []), default));
        StringAssert.Contains(refused.Message, "Invalid Path");
    }

    [TestMethod]
    public async Task EnsureFolder_FindsOrMakesEachLevel()
    {
        var wiki = new FakeWiki();
        wiki.Folders.Add((1, 0, "internal"));
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        var created = new List<string>();

        int id = await client.EnsureFolderAsync(["internal", "third-party", "page"], created, default);

        CollectionAssert.AreEqual(new[] { "third-party", "page" }, created);
        Assert.AreEqual(wiki.Folders.Single(folder => folder.Slug == "page").Id, id);
        Assert.AreEqual(wiki.Folders.Single(folder => folder.Slug == "third-party").Id, wiki.Folders.Single(folder => folder.Slug == "page").Parent);
    }

    [TestMethod]
    public async Task AProxysErrorPage_IsNeitherAnotherWikiNorTheKey_AndKeepsTheScripts()
    {
        using var site = new WikiClient("https://wiki.local", "k", new FakeWiki { GatewayTimeoutOn = "localization", TimeOutBeforeTheWiki = true });
        WikiNoAnswerException lost = await Assert.ThrowsExactlyAsync<WikiNoAnswerException>(() => site.GetSiteAsync(default));
        StringAssert.Contains(lost.Message, "HTTP 504 Gateway Time-out");
        Assert.IsFalse(lost.Message.Contains("Wiki.js 3", StringComparison.Ordinal), lost.Message);

        using var key = new WikiClient("https://wiki.local", "k", new FakeWiki { GatewayTimeoutOn = "profile" });
        await Assert.ThrowsExactlyAsync<WikiNoAnswerException>(() => key.CheckKeyAsync(default));

        // Taken for "cannot read them", the update would clear them.
        var wiki = new FakeWiki { CanManagePages = true, GatewayTimeoutOn = "single(" };
        wiki.Scripts[5] = ("body { color: red }", "");
        using var scripts = new WikiClient("https://wiki.local", "k", wiki);
        await Assert.ThrowsExactlyAsync<WikiNoAnswerException>(() => scripts.PageScriptsAsync(5, default));
    }

    [TestMethod]
    public async Task Upload_SendsTheFolderFirstAndAPlainName()
    {
        var wiki = new FakeWiki();
        using var client = new WikiClient("https://wiki.local", "k", wiki);

        await client.UploadAsync(7, "shot.png", [1, 2, 3], default);

        var asset = wiki.Assets.Single();
        Assert.AreEqual((7, "shot.png", "image/png"), (asset.Folder, asset.Name, asset.ContentType));
        Assert.IsFalse(wiki.LastUploadDisposition!.Contains("filename*", StringComparison.Ordinal), wiki.LastUploadDisposition);
    }
}

[TestClass]
public sealed class WikiImporterTests
{
    private string _folder = "";

    [TestInitialize]
    public void CreateFolder() => _folder = Directory.CreateTempSubdirectory("fbctools-wiki-").FullName;

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    private sealed class Report : IToolReport
    {
        public List<string> Lines { get; } = [];

        public void Output(string line) => Lines.Add(line);

        public void Progress(long done, long total)
        {
        }
    }

    private string WriteDocument()
    {
        string assets = Directory.CreateDirectory(Path.Combine(_folder, "Notes.assets")).FullName;
        File.WriteAllBytes(Path.Combine(assets, "image-1.png"), [1]);
        File.WriteAllBytes(Path.Combine(assets, "Ekran Görüntüsü.PNG"), [2]);
        string markdown = Path.Combine(_folder, "Notes.md");
        File.WriteAllText(markdown, """
            # Release Notes

            ![one](Notes.assets/image-1.png)
            <img src="Notes.assets/Ekran%20G%C3%B6r%C3%BCnt%C3%BCs%C3%BC.PNG" style="zoom:50%;">
            ![again](Notes.assets/image-1.png "Same")
            ![gone](Notes.assets/missing.png)
            ![web](https://example.com/logo.png)
            """);
        return markdown;
    }

    [TestMethod]
    public async Task Import_UploadsOnceRewritesTheLinksAndCreatesThePage()
    {
        var wiki = new FakeWiki();
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        WikiPageAddress address = WikiPageAddress.Parse("https://wiki.local/en/internal/notes");
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        ImportResult result = await WikiImporter.RunAsync(plan, address, "en", null, LinkStyle.Path, client, new Report(), default);

        Assert.AreEqual("Release Notes", wiki.Pages.Single().Title);
        Assert.AreEqual("internal/notes", wiki.Pages.Single().Path);
        CollectionAssert.AreEquivalent(new[] { "image-1.png", "ekran_goruntusu.png" }, wiki.Assets.Select(asset => asset.Name).ToArray());
        string content = wiki.Pages.Single().Content;
        StringAssert.StartsWith(content, "![one](/internal/notes/image-1.png)");
        StringAssert.Contains(content, """<img src="/internal/notes/ekran_goruntusu.png" style="zoom:50%;">""");
        StringAssert.Contains(content, """![again](/internal/notes/image-1.png "Same")""");
        StringAssert.Contains(content, "![gone](Notes.assets/missing.png)");
        StringAssert.Contains(content, "![web](https://example.com/logo.png)");
        Assert.IsFalse(content.Contains("# Release Notes", StringComparison.Ordinal));
        CollectionAssert.AreEqual(new[] { "Notes.assets/missing.png" }, result.Missing.ToArray());
        Assert.IsTrue(result.Created);
        Assert.AreEqual("https://wiki.local/en/internal/notes", result.PageUrl);
    }

    [TestMethod]
    public async Task Update_KeepsTheDescriptionTagsAndPublishing()
    {
        var wiki = new FakeWiki();
        wiki.Pages.Add((5, "internal/notes", "en", "Old", "old text", "Kept description", false, ["kept"]));
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        WikiPage existing = (await client.FindPageAsync("internal/notes", "en", default))!;
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        ImportResult result = await WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", existing, LinkStyle.FullUrl, client, new Report(), default);

        var page = wiki.Pages.Single();
        Assert.AreEqual(("Release Notes", "Kept description", false), (page.Title, page.Description, page.IsPublished));
        CollectionAssert.AreEqual(new[] { "kept" }, page.Tags);
        StringAssert.Contains(page.Content, "https://wiki.local/internal/notes/image-1.png");
        Assert.IsFalse(result.Created);
    }

    [TestMethod]
    public async Task ANewPage_DoesNotReplaceFilesAlreadyInItsFolder()
    {
        var wiki = new FakeWiki();
        wiki.Folders.Add((1, 0, "internal"));
        wiki.Folders.Add((2, 1, "notes"));
        wiki.Assets.Add((2, "image-1.png", "image/png", [9]));
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        await WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", null, LinkStyle.Path, client, new Report(), default);

        CollectionAssert.AreEqual(new byte[] { 9 }, wiki.Assets.Single(asset => asset.Name == "image-1.png").Bytes);
        StringAssert.Contains(wiki.Pages.Single().Content, "![one](/internal/notes/image-1-2.png)");
    }

    [TestMethod]
    public async Task Update_WritesThePageAtItsOwnPath_KeepsItsScripts_AndReplacesItsImages()
    {
        var wiki = new FakeWiki { CanManagePages = true };
        wiki.Pages.Add((5, "Internal/Notes", "en", "Old", "old text", "", true, []));
        wiki.Scripts[5] = ("body { color: red }", "alert(1)");
        wiki.Folders.Add((1, 0, "internal"));
        wiki.Folders.Add((2, 1, "notes"));
        wiki.Assets.Add((2, "image-1.png", "image/png", [9]));
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        WikiPage existing = (await client.FindPageAsync("internal/notes", "en", default))!;
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        ImportResult result = await WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", existing, LinkStyle.Path, client, new Report(), default);

        Assert.AreEqual("Internal/Notes", wiki.Pages.Single().Path);
        Assert.AreEqual("https://wiki.local/en/Internal/Notes", result.PageUrl);
        Assert.AreEqual(("body { color: red }", "alert(1)"), wiki.UpdatedScripts);
        CollectionAssert.AreEqual(new byte[] { 1 }, wiki.Assets.Single(asset => asset.Name == "image-1.png").Bytes);
    }

    [TestMethod]
    public async Task APageOfAnotherEditor_IsNotReplaced()
    {
        var wiki = new FakeWiki();
        wiki.Pages.Add((5, "internal/notes", "en", "Old", "<p>old</p>", "", true, []));
        wiki.ContentTypes[5] = "html";
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        WikiPage existing = (await client.FindPageAsync("internal/notes", "en", default))!;
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        await Assert.ThrowsExactlyAsync<WikiException>(
            () => WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", existing, LinkStyle.Path, client, new Report(), default));

        Assert.AreEqual("<p>old</p>", wiki.Pages.Single().Content);
        Assert.IsEmpty(wiki.Assets);
    }

    [TestMethod]
    public async Task ANewPageWhoseAnswerIsLost_IsReadBack()
    {
        var wiki = new FakeWiki { GatewayTimeoutOn = "create(content" };
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);
        var report = new Report();

        ImportResult result = await WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", null, LinkStyle.Path, client, report, default);

        Assert.IsTrue(result.Created);
        Assert.HasCount(1, wiki.Pages);
        Assert.IsTrue(report.Lines.Any(line => line.Contains("HTTP 504 Gateway Time-out", StringComparison.Ordinal) && line.Contains("page is saved", StringComparison.Ordinal)),
            string.Join("\n", report.Lines));
    }

    [TestMethod]
    public async Task ANewPageTheWikiNeverGot_StaysAFailure()
    {
        var wiki = new FakeWiki { GatewayTimeoutOn = "create(content", TimeOutBeforeTheWiki = true };
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        WikiNoAnswerException lost = await Assert.ThrowsExactlyAsync<WikiNoAnswerException>(
            () => WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", null, LinkStyle.Path, client, new Report(), default));

        StringAssert.Contains(lost.Message, "HTTP 504 Gateway Time-out");
        Assert.IsEmpty(wiki.Pages);
    }

    [TestMethod]
    public async Task AnUpdateWhoseAnswerIsLost_IsToldByItsTimeOfSaving()
    {
        var saved = new FakeWiki { GatewayTimeoutOn = "update(id" };
        saved.Pages.Add((5, "internal/notes", "en", "Old", "old text", "", true, []));
        using var client = new WikiClient("https://wiki.local", "k", saved);
        WikiPage existing = (await client.FindPageAsync("internal/notes", "en", default))!;
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);
        var address = WikiPageAddress.Parse("https://wiki.local/en/internal/notes");

        ImportResult result = await WikiImporter.RunAsync(plan, address, "en", existing, LinkStyle.Path, client, new Report(), default);

        Assert.IsFalse(result.Created);
        Assert.AreEqual("Release Notes", saved.Pages.Single().Title);

        // Saved again by Wiki.js itself after the page was first read (a render, say): a lost
        // update that never got there is not taken for saved.
        var notSaved = new FakeWiki { GatewayTimeoutOn = "update(id", TimeOutBeforeTheWiki = true };
        notSaved.Pages.Add((5, "internal/notes", "en", "Old", "old text", "", true, []));
        using var other = new WikiClient("https://wiki.local", "k", notSaved);
        WikiPage read = (await other.FindPageAsync("internal/notes", "en", default))!;
        notSaved.UpdatedAt[5] = "2026-10-22T11:59:00.000Z";
        await Assert.ThrowsExactlyAsync<WikiNoAnswerException>(
            () => WikiImporter.RunAsync(plan, address, "en", read, LinkStyle.Path, other, new Report(), default));
        Assert.AreEqual("old text", notSaved.Pages.Single().Content);

        // No time from the wiki: whether it was saved cannot be told, and is not claimed.
        var timeless = new FakeWiki { GatewayTimeoutOn = "update(id", NoUpdatedAt = true };
        timeless.Pages.Add((5, "internal/notes", "en", "Old", "old text", "", true, []));
        using var third = new WikiClient("https://wiki.local", "k", timeless);
        WikiPage noTime = (await third.FindPageAsync("internal/notes", "en", default))!;
        WikiException unknown = await Assert.ThrowsExactlyAsync<WikiException>(
            () => WikiImporter.RunAsync(plan, address, "en", noTime, LinkStyle.Path, third, new Report(), default));
        StringAssert.Contains(unknown.Message, "could not be read back");
    }

    [TestMethod]
    public async Task ABrokenConnection_IsReadBackToo_AndAWikiThatStaysDown_IsNotClaimed()
    {
        var wiki = new FakeWiki { GatewayTimeoutOn = "create(content", DropConnection = true };
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);
        var address = WikiPageAddress.Parse("https://wiki.local/en/internal/notes");

        ImportResult result = await WikiImporter.RunAsync(plan, address, "en", null, LinkStyle.Path, client, new Report(), default);
        Assert.IsTrue(result.Created);

        var down = new FakeWiki { GatewayTimeoutOn = "create(content", DownAfterTimeout = true };
        using var other = new WikiClient("https://wiki.local", "k", down);
        WikiException unknown = await Assert.ThrowsExactlyAsync<WikiException>(
            () => WikiImporter.RunAsync(plan, address, "en", null, LinkStyle.Path, other, new Report(), default));
        StringAssert.Contains(unknown.Message, "could not be read back");
        StringAssert.Contains(unknown.Message, "https://wiki.local/en/internal/notes");
    }

    [TestMethod]
    public void AnImageWithoutAnExtension_GetsOneFromItsBytes()
    {
        string image = Path.Combine(_folder, "Screenshot");
        File.WriteAllBytes(image, [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        Assert.AreEqual("screenshot.png", AssetNames.ForUpload([image])[image]);
    }

    [TestMethod]
    public async Task AnUploadTheServerDidNotKeep_StopsBeforeThePage()
    {
        var wiki = new FakeWiki { DropUploads = true };
        using var client = new WikiClient("https://wiki.local", "k", wiki);
        ImportPlan plan = WikiImporter.Prepare(WriteDocument(), removeFirstHeading: true, Encoding.Latin1);

        await Assert.ThrowsExactlyAsync<WikiException>(
            () => WikiImporter.RunAsync(plan, WikiPageAddress.Parse("https://wiki.local/en/internal/notes"), "en", null, LinkStyle.Path, client, new Report(), default));

        Assert.IsEmpty(wiki.Pages);
    }
}

[TestClass]
public sealed class WikiSettingsTests
{
    private sealed class ReversingProtector : ISecretProtector
    {
        public string Protect(string secret) => new([.. secret.Reverse()]);

        public string? Unprotect(string protectedText) => new([.. protectedText.Reverse()]);
    }

    [TestMethod]
    public void Settings_KeepKeysProtectedAndTheLastTargetPerFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fbctools-wiki-{Guid.NewGuid():N}.json");
        try
        {
            var protector = new ReversingProtector();
            var settings = new WikiSettings();
            settings.SetKey("https://wiki.local", "abc123", protector);
            settings.SetTarget(@"C:\docs\a.md", "https://wiki.local/en/a");
            settings.SetTarget(@"C:\DOCS\A.md", "https://wiki.local/en/b");
            settings.Save(path);

            WikiSettings loaded = WikiSettings.Load(path);

            Assert.AreEqual("abc123", loaded.KeyFor("https://WIKI.local", protector));
            Assert.IsFalse(File.ReadAllText(path).Contains("abc123", StringComparison.Ordinal));
            Assert.AreEqual("https://wiki.local/en/b", loaded.TargetFor(@"c:\docs\a.md"));
            Assert.AreEqual(LinkStyle.Path, loaded.LinkStyle);
            Assert.IsTrue(loaded.RemoveFirstHeading);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void Update_KeepsWhatAnotherImportSavedMeanwhile_AndNullListsReadAsEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fbctools-wiki-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "sites": null, "targets": null }""");
        try
        {
            WikiSettings mine = WikiSettings.Load(path);
            Assert.IsEmpty(mine.Sites);

            WikiSettings.Update(path, other => other.SetTarget(@"C:\docs\b.md", "https://wiki.local/en/b"));
            WikiSettings.Update(path, fresh => fresh.SetTarget(@"C:\docs\a.md", "https://wiki.local/en/a"));

            WikiSettings loaded = WikiSettings.Load(path);
            Assert.AreEqual("https://wiki.local/en/a", loaded.TargetFor(@"C:\docs\a.md"));
            Assert.AreEqual("https://wiki.local/en/b", loaded.TargetFor(@"C:\docs\b.md"));
            Assert.IsEmpty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.tmp"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void BrokenSettings_AreReportedNotReplaced()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fbctools-wiki-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ broken");
        try
        {
            Assert.ThrowsExactly<WikiException>(() => WikiSettings.Load(path));
            Assert.AreEqual("{ broken", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
