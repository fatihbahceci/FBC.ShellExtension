using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>Rename from IMDb: names, groups and OMDb's answers (ADR-0046).</summary>
[TestClass]
public sealed class MovieNamesTests
{
    private static readonly MovieInfo s_matrix = new("The Matrix", "1999", "tt0133093");

    [TestMethod]
    public void Tool_OpensTheEditorsWindowFromVersion16()
    {
        var config = new MenuConfig { Version = 15, Items = [new CommandEntry { Label = "IMDb", Tool = BuiltinTool.RenameFromImdb }] };

        Assert.AreEqual(16, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesEditorWindow(BuiltinTool.RenameFromImdb));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.RenameFromImdb, ItemKind.File));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.RenameFromImdb, ItemKind.Background));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.RenameFromImdb, ItemKind.Drive));
    }

    [TestMethod]
    [DataRow("Inception.2010.1080p.BluRay.x264-SPARKS", "Inception", "2010", null)]
    [DataRow("Blade.Runner.2049.2017.2160p", "Blade Runner 2049", "2017", null)]
    [DataRow("Blade Runner 2049", "Blade Runner 2049", null, null)]
    [DataRow("1917.2019.1080p", "1917", "2019", null)]
    [DataRow("2012", "2012", null, null)]
    [DataRow("2001.A.Space.Odyssey.1968", "2001 A Space Odyssey", "1968", null)]
    [DataRow("The Matrix (1999)", "The Matrix", "1999", null)]
    [DataRow("(500) Days of Summer (2009)", "(500) Days of Summer", "2009", null)]
    [DataRow("[YTS.MX] Some_Film 2015 [720p]", "Some Film", "2015", null)]
    [DataRow("Film (Extended) 2015", "Film", "2015", null)]
    [DataRow("Inception 1080p BluRay", "Inception", null, null)]
    [DataRow("Film.1080p.1920x1080", "Film", null, null)]
    [DataRow("Spider-Man.2002.DVDRip", "Spider-Man", "2002", null)]
    [DataRow("Kill.Bill.Vol.1.2003.CD1", "Kill Bill Vol 1", "2003", "cd1")]
    [DataRow("Harry.Potter.and.the.Deathly.Hallows.Part.1.2010.720p", "Harry Potter and the Deathly Hallows Part 1", "2010", null)]
    [DataRow("Some.Film.2010.Part.02", "Some Film", "2010", "part2")]
    [DataRow("Some Film CD2", "Some Film", null, "cd2")]
    [DataRow("Some Film Part 2", "Some Film Part 2", null, null)]
    [DataRow("Movie.2010.PAL.DVD9", "Movie", "2010", null)]
    [DataRow("Movie.2010.DVD2", "Movie", "2010", "dvd2")]
    [DataRow("Film.2027", "Film", "2027", null)]
    [DataRow("Film.2028.1080p", "Film 2028", null, null)]
    [DataRow("Movie.DVD-9", "Movie", null, null)]
    [DataRow("Movie.DVD.5.x264", "Movie", null, null)]
    [DataRow("Movie CD\uFF11", "Movie CD\uFF11", null, null)]
    [DataRow("Film 20\uFF11\uFF10", "Film 20\uFF11\uFF10", null, null)]
    public void Guess_FindsTheTitleTheYearAndThePart(string name, string title, string? year, string? part)
    {
        Assert.AreEqual(new MovieGuess(title, year, part), MovieNames.Guess(name, 2026));
    }

    [TestMethod]
    public void Group_PutsSubtitlesWithTheirVideo()
    {
        IReadOnlyList<MovieGroup> groups = MovieNames.Group([
            File(@"C:\m\Film.2010.en.srt"), File(@"C:\m\Film.2010.mkv"), File(@"C:\m\Film.2010.tr.forced.srt"),
            File(@"C:\m\Film.2010.srt"), File(@"C:\m\Film.2010.MP4"), File(@"C:\m\Other.Film.en.srt"),
            File(@"C:\m\Up.srt"), File(@"C:\m\Lone.pt-BR.forced.srt"), File(@"C:\n\Film.2010.srt"),
            Folder(@"C:\m\Extras"), File(@"C:\m\Film.2010.mkv")]);

        string Describe(MovieGroup group) => $"{group.BaseName}: " + string.Join(", ", group.Files.Select(file => $"{file.Item.Name}={file.Suffix}"));
        CollectionAssert.AreEqual(new[]
        {
            "Film.2010: Film.2010.mkv=, Film.2010.MP4=, Film.2010.en.srt=.en, Film.2010.tr.forced.srt=.tr.forced, Film.2010.srt=",
            "Other.Film: Other.Film.en.srt=.en",
            "Up: Up.srt=",
            "Lone: Lone.pt-BR.forced.srt=.pt-BR.forced",
            "Film.2010: Film.2010.srt=",
        }, groups.Select(Describe).ToArray());
        Assert.AreEqual(new MovieGuess("Film", "2010", null), groups[0].Guess);
    }

    [TestMethod]
    [DataRow("Inception.2010.720p.WEB-DL.English", "Inception.2010.720p.WEB-DL", ".English")]
    [DataRow("Film.2010.es-419", "Film.2010", ".es-419")]
    [DataRow("Film_2010_en", "Film_2010", ".en")]
    [DataRow("Film.2010-en", "Film.2010", ".en")]
    [DataRow("Film 2010 en forced", "Film 2010", ".en.forced")]
    [DataRow("Film.2010.it", "Film.2010", ".it")]
    [DataRow("Spider-Man.en", "Spider-Man", ".en")]
    [DataRow("Lone.pt-BR.forced", "Lone", ".pt-BR.forced")]
    [DataRow("Dr.No", "Dr.No", "")]
    [DataRow("Dr.No.en", "Dr.No", ".en")]
    [DataRow("Seven.Days.in.May", "Seven.Days.in.May", "")]
    [DataRow("Johnny.English", "Johnny.English", "")]
    [DataRow("Rocky.VI", "Rocky.VI", "")]
    [DataRow("Up", "Up", "")]
    [DataRow("Man of Tai Chi", "Man of Tai Chi", "")]
    [DataRow("Man.of.Tai.Chi", "Man.of.Tai.Chi", "")]
    [DataRow("Shang-Chi", "Shang-Chi", "")]
    [DataRow("Sci-Fi", "Sci-Fi", "")]
    [DataRow("Made in UK", "Made in UK", "")]
    [DataRow("The.Hi-Lo", "The.Hi-Lo", "")]
    [DataRow("Film en", "Film en", "")]
    [DataRow("Film.2010.tr-TR", "Film.2010", ".tr-TR")]
    [DataRow("Film.2010-pt-BR", "Film.2010", ".pt-BR")]
    [DataRow("Film.2010-tr-TR", "Film.2010", ".tr-TR")]
    [DataRow("Film.2010.NF-en", "Film.2010.NF", ".en")]
    [DataRow("Film.2010.DTS-fr", "Film.2010.DTS", ".fr")]
    [DataRow("Spider-Man-en", "Spider-Man-en", "")]
    public void SplitSubtitle_KeepsTheLanguageNotTheTitlesLastWord(string stem, string name, string suffix)
    {
        Assert.AreEqual((name, suffix), MovieNames.SplitSubtitle(stem));
    }

    [TestMethod]
    public void Group_GivesAVideoItsSubtitlesWhateverTheirSeparator()
    {
        IReadOnlyList<MovieGroup> groups = MovieNames.Group([
            File(@"C:\m\Inception.2010.mkv"), File(@"C:\m\Inception.2010_en.srt"), File(@"C:\m\Inception.2010 tr.srt"),
            File(@"C:\m\Inception.2010-English.srt")]);

        Assert.AreEqual(1, groups.Count);
        CollectionAssert.AreEqual(new[] { "", ".en", ".tr", ".English" }, groups[0].Files.Select(file => file.Suffix).ToArray());

        // A longer video's name, once the tag is cut off, wins over a shorter one that only starts it.
        IReadOnlyList<MovieGroup> matrix = MovieNames.Group([
            File(@"C:\m\The.Matrix.mkv"), File(@"C:\m\The.Matrix.Reloaded.mkv"), File(@"C:\m\The.Matrix.Reloaded_en.srt"),
            File(@"C:\m\The.Matrix.en.srt")]);
        Assert.AreEqual(".en", matrix.Single(group => group.BaseName == "The.Matrix").Files[1].Suffix);
        Assert.AreEqual("The.Matrix.Reloaded_en.srt", matrix.Single(group => group.BaseName == "The.Matrix.Reloaded").Files[1].Item.Name);
        Assert.AreEqual(".en", matrix.Single(group => group.BaseName == "The.Matrix.Reloaded").Files[1].Suffix);
    }

    [TestMethod]
    public void Group_GivesASubtitleTheLongestVideoName()
    {
        IReadOnlyList<MovieGroup> groups = MovieNames.Group([File(@"C:\m\Film.mkv"), File(@"C:\m\Film 2.mkv"), File(@"C:\m\Film 2.en.srt")]);

        Assert.AreEqual(1, groups.Single(group => group.BaseName == "Film").Files.Count);
        Assert.AreEqual(".en", groups.Single(group => group.BaseName == "Film 2").Files[1].Suffix);
    }

    [TestMethod]
    public void NewName_FillsTheTemplateAndKeepsSuffixPartAndExtension()
    {
        MovieGroup group = MovieNames.Group([File(@"C:\m\Kill.Bill.2003.CD1.avi"), File(@"C:\m\Kill.Bill.2003.CD1.en.srt")])[0];
        var killBill = new MovieInfo("Kill Bill: Vol. 1", "2003", "tt0266697");

        Assert.AreEqual("Kill Bill - Vol. 1 (2003) [imdbid-tt0266697] - cd1.avi", MovieNames.NewName(MovieNames.DefaultFormat, killBill, group, group.Files[0]));
        Assert.AreEqual("Kill Bill - Vol. 1 (2003) [imdbid-tt0266697] - cd1.en.srt", MovieNames.NewName(MovieNames.DefaultFormat, killBill, group, group.Files[1]));

        MovieGroup plain = MovieNames.Group([File(@"C:\m\film.MKV")])[0];
        Assert.AreEqual("Face-Off 1997.MKV", MovieNames.NewName("{TITLE} {Year}", new MovieInfo("Face/Off", "1997", "tt0119094"), plain, plain.Files[0]));
        Assert.AreEqual("Who Is It.MKV", MovieNames.NewName("{title}", new MovieInfo("Who <Is> \"It\"?..", "", ""), plain, plain.Files[0]));

        // Plex reads its own form; the braces around a token stay as they are.
        const string plex = "{title} ({year}) {imdb-{imdbid}}";
        Assert.IsNull(MovieNames.FormatProblem(plex));
        Assert.AreEqual("The Matrix (1999) {imdb-tt0133093}.MKV", MovieNames.NewName(plex, s_matrix, plain, plain.Files[0]));
    }

    [TestMethod]
    public void FormatProblem_KnowsOnlyTheFilmsTokens()
    {
        Assert.IsNull(MovieNames.FormatProblem(MovieNames.DefaultFormat));
        Assert.AreEqual(Strings.ImdbFormatEmpty(), MovieNames.FormatProblem(" "));
        Assert.AreEqual(Strings.ImdbTokenUnknown("{n}"), MovieNames.FormatProblem("{title} {n}"));
        StringAssert.Contains(Strings.ImdbTokenUnknown("{n}"), "{imdbid}");
    }

    [TestMethod]
    public void PreviewNames_ChecksTheNamesLikeAdvancedRename()
    {
        SelectedItem a = File(@"C:\m\a.mkv"), b = File(@"C:\m\b.mkv"), c = File(@"C:\m\c.mkv"), d = File(@"C:\m\d.mkv");
        RenamePreview preview = FileTools.PreviewNames(
            [(a, "Film.mkv"), (b, "film.mkv"), (c, "Taken.mkv"), (d, "d.mkv")],
            path => path.EndsWith(@"\Taken.mkv", StringComparison.OrdinalIgnoreCase));

        CollectionAssert.AreEqual(
            new[] { Strings.RenameProblemDuplicate(), Strings.RenameProblemDuplicate(), Strings.RenameProblemExists(), null },
            preview.Rows.Select(row => row.Problem).ToArray());
        Assert.IsFalse(preview.CanRename);
        Assert.IsTrue(FileTools.PreviewNames([(a, "Film.mkv"), (d, "d.mkv")], _ => false).CanRename);
    }

    [TestMethod]
    public void Read_TakesSearchesAndTitles()
    {
        CollectionAssert.AreEqual(
            new[] { s_matrix, new MovieInfo("The Matrix Reloaded", "2003", "tt0234215") },
            Omdb.Read(200, """{"Search":[{"Title":"The Matrix","Year":"1999","imdbID":"tt0133093","Type":"movie","Poster":"N/A"},{"Title":"The Matrix Reloaded","Year":"2003","imdbID":"tt0234215","Type":"movie"},{"Title":"The Matrix","Year":"1999","imdbID":"tt0133093"}],"totalResults":"3","Response":"True"}""").Films.ToArray());
        CollectionAssert.AreEqual(new[] { s_matrix },
            Omdb.Read(200, """{"Title":"The Matrix","Year":"1999","Rated":"R","imdbID":"tt0133093","Ratings":[{"Source":"x","Value":"y"}],"Response":"True"}""").Films.ToArray());
        Assert.AreEqual(OmdbFilms.None, Omdb.Read(200, """{"Response":"False","Error":"Movie not found!"}"""));
        Assert.AreEqual(new OmdbFilms([], More: true).More, Omdb.Read(200, """{"Response":"False","Error":"Too many results."}""").More);
    }

    [TestMethod]
    [DataRow(401, """{"Response":"False","Error":"Invalid API key!"}""", OmdbFailure.InvalidKey)]
    [DataRow(401, """{"Response":"False","Error":"Request limit reached!"}""", OmdbFailure.LimitReached)]
    [DataRow(200, """{"Response":"False","Error":"Something went wrong."}""", OmdbFailure.Other)]
    [DataRow(200, "<html>", OmdbFailure.Other)]
    [DataRow(503, "Service Unavailable", OmdbFailure.Other)]
    public void Read_StopsOnTheKeyTheLimitAndStrangeAnswers(int status, string body, OmdbFailure failure)
    {
        OmdbException ex = Assert.ThrowsExactly<OmdbException>(() => Omdb.Read(status, body));

        Assert.AreEqual(failure, ex.Failure);
        if (status == 503)
        {
            Assert.AreEqual(Strings.ImdbHttpError(503), ex.Message);
        }
    }

    [TestMethod]
    public void Addresses_CarryTheEscapedQueryAndYear()
    {
        Assert.AreEqual("https://www.omdbapi.com/?apikey=a%26b&type=movie&s=Kill%20Bill%3A%20Vol.%201&y=2003",
            Omdb.SearchUri(" a&b ", "Kill Bill: Vol. 1", "2003").AbsoluteUri);
        Assert.AreEqual("https://www.omdbapi.com/?apikey=k&type=movie&i=tt0133093", Omdb.IdUri("k", "tt0133093").AbsoluteUri);
        Assert.AreEqual("tt0111161", Omdb.ImdbIdIn("https://www.imdb.com/title/TT0111161/?ref_=nv"));
        Assert.IsNull(Omdb.ImdbIdIn("tt123"));
        Assert.IsNull(Omdb.ImdbIdIn("The Matrix"));
    }

    [TestMethod]
    public async Task Lookup_TakesOnlyWhatIsCertain()
    {
        var reloaded = new MovieInfo("The Matrix Reloaded", "2003", "tt0234215");

        // With a year: the exact title, or the one film of that year.
        Assert.AreEqual(s_matrix, (await Lookup(new("The Matrix", "1999", null), ("t:1999", Films(s_matrix)))).Match);
        Assert.AreEqual(s_matrix, (await Lookup(new("Matrix", "1999", null), ("s:1999", Films(s_matrix)))).Match);
        MovieLookup several = await Lookup(new("Matrix", "1999", null), ("s:1999", Films(s_matrix, reloaded)));
        Assert.IsNull(several.Match);
        Assert.AreEqual(2, several.Candidates.Count);

        // A year OMDb does not have: the title's films are offered, even one.
        MovieLookup otherYear = await Lookup(new("The Matrix", "1998", null), ("s:", Films(s_matrix)));
        Assert.IsNull(otherYear.Match);
        CollectionAssert.AreEqual(new[] { s_matrix }, otherYear.Candidates.ToArray());

        // Without one: the search has to have just that film; the title answer alone is only the best known.
        Assert.AreEqual(s_matrix, (await Lookup(new("The Matrix", null, null), ("t:", Films(s_matrix)), ("s:", Films(s_matrix)))).Match);
        Assert.AreEqual(s_matrix, (await Lookup(new("The Matrix", null, null), ("s:", Films(s_matrix)))).Match);
        MovieLookup open = await Lookup(new("Matrix", null, null), ("t:", Films(s_matrix)), ("s:", Films(reloaded, s_matrix)));
        Assert.IsNull(open.Match);
        CollectionAssert.AreEqual(new[] { s_matrix, reloaded }, open.Candidates.ToArray());
        Assert.AreEqual(0, (await Lookup(new("Nothing", null, null))).Candidates.Count);

        var it = new MovieInfo("It", "2017", "tt1396484");
        foreach (OmdbFilms search in new[] { OmdbFilms.None, new OmdbFilms([], More: true), new OmdbFilms([reloaded], More: true) })
        {
            MovieLookup alone = await Lookup(new("It", null, null), ("t:", Films(it)), ("s:", search));
            Assert.IsNull(alone.Match);
            Assert.AreEqual(it, alone.Candidates[0]);
        }

        Assert.IsNull((await Lookup(new("It", "2017", null), ("s:2017", new OmdbFilms([it], More: true)))).Match);

        // With a year and nothing listed in any year: the title's best known film is offered.
        MovieLookup tooMany = await Lookup(new("It", "2016", null), ("s:2016", new OmdbFilms([], More: true)), ("s:", new OmdbFilms([], More: true)), ("t:", Films(it)));
        Assert.IsNull(tooMany.Match);
        CollectionAssert.AreEqual(new[] { it }, tooMany.Candidates.ToArray());
    }

    private static OmdbFilms Films(params MovieInfo[] films) => new(films);

    /// <summary>OMDb as a table: "t:1999" is a title asked with that year, "s:" a search without one.</summary>
    private static Task<MovieLookup> Lookup(MovieGuess guess, params (string Query, OmdbFilms Answer)[] answers) =>
        Omdb.LookupAsync(guess, "key", (uri, _) =>
        {
            Dictionary<string, string> query = uri.Query.TrimStart('?').Split('&')
                .Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));
            Assert.AreEqual("key", query["apikey"]);
            string field = query.ContainsKey("t") ? "t" : "s";
            Assert.AreEqual(guess.Title, query[field]);
            string asked = $"{field}:{query.GetValueOrDefault("y")}";
            return Task.FromResult(answers.FirstOrDefault(answer => answer.Query == asked).Answer ?? OmdbFilms.None);
        }, CancellationToken.None);

    [TestMethod]
    public void MovieSources_TakesTheFoldersVideosAndSubtitles()
    {
        string root = Path.Combine(Path.GetTempPath(), "fbc-movies-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Sub"));
            foreach (string name in new[] { "a.mkv", "a.en.srt", "notes.txt", Path.Combine("Sub", "b.mp4") })
            {
                System.IO.File.WriteAllText(Path.Combine(root, name), "");
            }

            string other = Path.Combine(root, "notes.txt");
            IReadOnlyList<SelectedItem> top = FileTools.MovieSources([Folder(root), File(other), File(Path.Combine(root, "a.mkv"))], false, out List<string> errors);
            CollectionAssert.AreEqual(new[] { "a.en.srt", "a.mkv", "notes.txt" }, top.Select(item => Path.GetFileName(item.Path)).ToArray());
            Assert.AreEqual(0, errors.Count);

            IReadOnlyList<SelectedItem> deep = FileTools.MovieSources([Folder(root)], true, out _);
            CollectionAssert.AreEqual(new[] { "a.en.srt", "a.mkv", "b.mp4" }, deep.Select(item => Path.GetFileName(item.Path)).ToArray());

            FileTools.MovieSources([Folder(Path.Combine(root, "Missing"))], false, out List<string> missing);
            Assert.AreEqual(1, missing.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
