using System.Globalization;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>A film as OMDb gives it (ADR-0046).</summary>
public sealed record MovieInfo(string Title, string Year, string ImdbId);

/// <summary>
/// What a file's name says of its film: the title to look up, the year when the name has one,
/// and the part (<c>cd1</c>) when the film comes in pieces.
/// </summary>
public sealed record MovieGuess(string Title, string? Year, string? Part);

/// <summary>One file of a film; <paramref name="Suffix"/> is what follows the video's name in a subtitle's (<c>.en</c>, <c>.tr.forced</c>).</summary>
public sealed record MovieFile(SelectedItem Item, string Suffix);

/// <summary>A video and the subtitles named after it, in one folder: looked up once, renamed together.</summary>
public sealed record MovieGroup(string BaseName, MovieGuess Guess, IReadOnlyList<MovieFile> Files);

/// <summary>
/// Rename from IMDb's names (ADR-0046): the film a file's name points at, the files that go
/// together, and the new name a found film gives each. Nothing here touches the disk.
/// </summary>
public static partial class MovieNames
{
    /// <summary>The name Jellyfin (and Emby) read the IMDb id from; Plex reads <c>{imdb-tt…}</c>, a template of its own.</summary>
    public const string DefaultFormat = "{title} ({year}) [imdbid-{imdbid}]";

    /// <summary>
    /// The videos a folder is searched for. <c>.vob</c> is not one: a DVD's files have to keep
    /// their names. A file given directly is taken whatever its extension.
    /// </summary>
    public static IReadOnlySet<string> VideoExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".ts", ".3gp",
    };

    public static IReadOnlySet<string> SubtitleExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".srt", ".sub", ".idx", ".ass", ".ssa", ".vtt", ".sup", ".smi",
    };

    /// <summary>The language codes a lone subtitle's name may end in, which it keeps (<c>Film.en.srt</c>).</summary>
    private static readonly HashSet<string> s_languageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "tr", "de", "fr", "es", "it", "pt", "ru", "ja", "ko", "zh", "ar", "nl", "sv", "no", "nb", "da", "fi", "pl",
        "cs", "hu", "el", "he", "ro", "bg", "uk", "hr", "sr", "sl", "sk", "fa", "hi", "th", "vi", "id", "ms", "az",
        "eng", "tur", "ger", "deu", "fre", "fra", "spa", "ita", "por", "rus", "jpn", "kor", "chi", "zho", "ara", "dut",
        "nld", "swe", "nor", "dan", "fin", "pol", "cze", "ces", "hun", "gre", "ell", "heb", "rum", "ron", "bul", "ukr",
        "hrv", "srp", "slv", "slo", "slk", "per", "fas", "hin", "tha", "vie", "ind", "may", "msa", "aze",
    };

    /// <summary>
    /// Codes that are words a title may end in too ("Dr. No", "Seven Days in May", "Rocky VI",
    /// "Man of Tai Chi", "Sci-Fi"). Like a language's name ("Johnny English"), and like any tag
    /// after a space or a dash, one counts as a language only after a year or a release word:
    /// <c>Film.2010.720p.English.srt</c>, not <c>Dr.No.srt</c>.
    /// </summary>
    private static readonly HashSet<string> s_wordLikeCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "no", "it", "he", "el", "hi", "id", "ms", "ar", "da", "vi", "fi", "uk", "sr", "fin", "per", "ind", "dan", "nor",
        "may", "hun", "slo", "ara", "bul", "pol", "gre", "chi", "vie", "ron", "rum", "por",
    };

    private static readonly HashSet<string> s_languageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "english", "turkish", "turkce", "türkçe", "german", "deutsch", "french", "francais", "français", "spanish", "espanol",
        "español", "italian", "portuguese", "brazilian", "russian", "japanese", "korean", "chinese", "arabic", "dutch",
        "swedish", "norwegian", "danish", "finnish", "polish", "czech", "hungarian", "greek", "hebrew", "romanian",
        "bulgarian", "ukrainian", "croatian", "serbian", "slovenian", "slovak", "persian", "farsi", "hindi", "thai",
        "vietnamese", "indonesian", "malay", "azerbaijani",
    };

    /// <summary>What a subtitle's name may end in besides its language, also kept (<c>Film.en.forced.srt</c>).</summary>
    private static readonly HashSet<string> s_subtitleFlags = new(StringComparer.OrdinalIgnoreCase) { "forced", "sdh", "cc", "default" };

    private const int MaxSubtitleTags = 3;

    private const int OldestFilm = 1888;

    public static bool IsMovieFile(string name)
    {
        string extension = WindowsPath.GetExtension(name);
        return VideoExtensions.Contains(extension) || SubtitleExtensions.Contains(extension);
    }

    /// <summary>
    /// The files in groups: every video with the subtitles in its folder whose name starts with
    /// the video's (<c>Film.mkv</c>, <c>Film.en.srt</c>); a subtitle without its video stands
    /// alone. Videos of one name (<c>Film.mkv</c>, <c>Film.mp4</c>) are one group. Folders are left out.
    /// </summary>
    public static IReadOnlyList<MovieGroup> Group(IEnumerable<SelectedItem> items)
    {
        List<SelectedItem> files = [.. items.Where(item => item.Kind == ItemKind.File).DistinctBy(item => item.Path, StringComparer.OrdinalIgnoreCase)];
        var groups = new List<MovieGroup>();
        foreach (IGrouping<string, SelectedItem> folder in files.GroupBy(item => item.Directory, StringComparer.OrdinalIgnoreCase))
        {
            var videos = new Dictionary<string, List<MovieFile>>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            var subtitles = new List<SelectedItem>();
            foreach (SelectedItem file in folder)
            {
                if (SubtitleExtensions.Contains(file.Extension))
                {
                    subtitles.Add(file);
                }
                else if (videos.TryGetValue(file.NameWithoutExtension, out List<MovieFile>? same))
                {
                    same.Add(new MovieFile(file, ""));
                }
                else
                {
                    videos[file.NameWithoutExtension] = [new MovieFile(file, "")];
                    order.Add(file.NameWithoutExtension);
                }
            }

            var subtitlesOf = order.ToDictionary(name => name, _ => new List<MovieFile>(), StringComparer.OrdinalIgnoreCase);
            var lone = new List<(SelectedItem Item, (string Base, string Suffix) Split)>();
            foreach (SelectedItem subtitle in subtitles)
            {
                // The longest video name wins: "Film 2" over "Film" for "Film 2.en.srt". A name
                // that is a video's once its tags are cut off counts too: "Film.2010_en.srt" is
                // Film.2010's, and "The.Matrix.Reloaded_en.srt" The.Matrix.Reloaded's, not The.Matrix's.
                string stem = subtitle.NameWithoutExtension;
                string? video = order.Where(name => stem.Length == name.Length
                        ? string.Equals(stem, name, StringComparison.OrdinalIgnoreCase)
                        : stem.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase))
                    .MaxBy(name => name.Length);
                (string Base, string Suffix) split = SplitSubtitle(stem);
                if (subtitlesOf.TryGetValue(split.Base, out List<MovieFile>? ofBase) && (video is null || split.Base.Length > video.Length))
                {
                    ofBase.Add(new MovieFile(subtitle, split.Suffix));
                }
                else if (video is not null)
                {
                    subtitlesOf[video].Add(new MovieFile(subtitle, stem[video.Length..]));
                }
                else
                {
                    lone.Add((subtitle, split));
                }
            }

            groups.AddRange(order.Select(name => new MovieGroup(name, Guess(name), [.. videos[name], .. subtitlesOf[name]])));
            foreach (var same in lone.GroupBy(each => each.Split.Base, StringComparer.OrdinalIgnoreCase))
            {
                groups.Add(new MovieGroup(same.Key, Guess(same.Key), [.. same.Select(each => new MovieFile(each.Item, each.Split.Suffix))]));
            }
        }

        return groups;
    }

    /// <summary>
    /// A lone subtitle's name split into the film's and the language and flags at its end (three
    /// at most), which the new name keeps, written with dots: <c>Film.2010_en.forced</c> →
    /// <c>Film.2010</c> and <c>.en.forced</c>. A tag follows a dot, an underscore, a space or a
    /// dash; a language may have a region (<c>pt-BR</c>, <c>es-419</c>). The name itself is never
    /// a tag: "Up.srt" stays "Up".
    /// </summary>
    internal static (string Base, string Suffix) SplitSubtitle(string stem)
    {
        string rest = stem;
        var tags = new List<string>();
        while (tags.Count < MaxSubtitleTags && LastTag(rest) is { } last && last.Before.Trim(' ', '.', '_', '-').Length > 0
            && TagKind(last.Tag) is { } kind
            && ((kind == SubtitleTag.Certain && last.Separator is '.' or '_') || HoldsYearOrRelease(last.Before)))
        {
            tags.Insert(0, last.Tag);
            rest = last.Before;
        }

        return (rest, string.Concat(tags.Select(tag => "." + tag)));
    }

    private enum SubtitleTag
    {
        Certain,

        /// <summary>A word as well; a tag only after a year or a release word.</summary>
        AfterRelease,
    }

    private static (string Before, char Separator, string Tag)? LastTag(string text)
    {
        // A region first: "pt-BR" is one tag, not "BR" after a dash. Only a language has one:
        // in "Film.2010.NF-en" the tag is "en".
        Match region = RegionTail().Match(text);
        if (region.Success && s_languageCodes.Contains(region.Value[..region.Value.IndexOf('-')]))
        {
            return (text[..(region.Index - 1)], text[region.Index - 1], region.Value);
        }

        int cut = text.LastIndexOfAny(['.', '_', ' ', '-']);
        return cut > 0 && cut < text.Length - 1 ? (text[..cut], text[cut], text[(cut + 1)..]) : null;
    }

    private static SubtitleTag? TagKind(string tag)
    {
        int dash = tag.IndexOf('-');
        if (dash > 0)
        {
            string language = tag[..dash];
            return !s_languageCodes.Contains(language) ? null
                : s_wordLikeCodes.Contains(language) ? SubtitleTag.AfterRelease
                : SubtitleTag.Certain;
        }

        return s_subtitleFlags.Contains(tag) ? SubtitleTag.Certain
            : s_wordLikeCodes.Contains(tag) || s_languageNames.Contains(tag) ? SubtitleTag.AfterRelease
            : s_languageCodes.Contains(tag) ? SubtitleTag.Certain
            : null;
    }

    private static bool HoldsYearOrRelease(string text)
    {
        string words = Separators().Replace(text, " ");
        return YearPattern().IsMatch(words) || ReleasePattern().IsMatch(words);
    }

    /// <summary>
    /// The film a name points at. The year is the last one in the name that is not where the name
    /// starts and not later than next year (a film may come out under next year's date):
    /// "Blade.Runner.2049.2017" is "Blade Runner 2049" of 2017, "1917.2019" is "1917". Without a
    /// year the title ends where release words begin (1080p, BluRay, x264). A part (<c>CD1</c>,
    /// <c>Part 2</c>) counts after the year; without one only cd, disc and dvd do, as "Part 2" may
    /// be in the title. DVD5 and DVD9 are the disc's size, not a part. Only ASCII digits count.
    /// </summary>
    public static MovieGuess Guess(string baseName) => Guess(baseName, DateTime.Today.Year);

    internal static MovieGuess Guess(string baseName, int thisYear)
    {
        string text = Separators().Replace(baseName, " ");
        Match? year = null;
        foreach (Match candidate in YearPattern().Matches(text))
        {
            int value = int.Parse(candidate.Value, CultureInfo.InvariantCulture);
            if (value >= OldestFilm && value <= thisYear + 1 && Tidy(text[..candidate.Index]).Length > 0)
            {
                year = candidate;
            }
        }

        string? part;
        int end;
        if (year is not null)
        {
            Match partMatch = PartPattern().Match(text, year.Index + year.Length);
            part = partMatch.Success ? Part(partMatch) : null;
            end = year.Index;
        }
        else
        {
            Match partMatch = DiscPattern().Match(text);
            part = partMatch.Success ? Part(partMatch) : null;
            Match release = ReleasePattern().Match(text);
            end = Math.Min(release.Success ? release.Index : text.Length, partMatch.Success ? partMatch.Index : text.Length);
        }

        string title = Tidy(Tags().Replace(text[..end], " "));
        title = Tidy(TrailingParentheses().Replace(title, ""));
        return new MovieGuess(title.Length > 0 ? title : Tidy(text), year?.Value, part);
    }

    private static string Part(Match match) =>
        match.Groups["kind"].Value.ToLowerInvariant() + int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

    /// <summary>One space between words, and no dash or opening bracket left at the end; "(500) Days" keeps its own.</summary>
    private static string Tidy(string text) => Spaces().Replace(text, " ").Trim(' ', '-').TrimEnd(' ', '-', '(', '[', '{');

    /// <summary>Why the name template cannot be used: empty, or a token other than <c>{title}</c>, <c>{year}</c> and <c>{imdbid}</c>.</summary>
    public static string? FormatProblem(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return Strings.ImdbFormatEmpty();
        }

        foreach (Match token in Token().Matches(format))
        {
            if (token.Groups["name"].Value.ToLowerInvariant() is not ("title" or "year" or "imdbid"))
            {
                return Strings.ImdbTokenUnknown(token.Value);
            }
        }

        return null;
    }

    /// <summary>
    /// The file's new name: the template filled with the film, then the part (" - cd1"), the
    /// subtitle's suffix (".en") and the file's own extension. The film's values are made fit
    /// for a name ("Mission: Impossible" → "Mission - Impossible"); the template is the user's
    /// own, and what it holds is checked with the rest.
    /// </summary>
    public static string NewName(string format, MovieInfo movie, MovieGroup group, MovieFile file)
    {
        string stem = Token().Replace(format, token => token.Groups["name"].Value.ToLowerInvariant() switch
        {
            "title" => Clean(movie.Title),
            "year" => Clean(movie.Year),
            "imdbid" => Clean(movie.ImdbId),
            _ => token.Value,
        });
        stem = Spaces().Replace(stem, " ").Trim().TrimEnd('.').TrimEnd();
        if (group.Guess.Part is { } part)
        {
            stem += " - " + part;
        }

        return stem + file.Suffix + file.Item.Extension;
    }

    /// <summary>A value fit for a file name: a colon becomes a dash, a slash one too, and what Windows refuses goes.</summary>
    public static string Clean(string value)
    {
        string text = value.Replace(": ", " - ", StringComparison.Ordinal).Replace(':', '-').Replace('/', '-').Replace('\\', '-');
        text = string.Concat(text.Where(c => c is not ('<' or '>' or '"' or '|' or '?' or '*') && !char.IsControl(c)));
        return Spaces().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"[._]+")]
    private static partial Regex Separators();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    // Not part of a word or a number: "1920x1080" and "x2010" are not years.
    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?:18|19|20)[0-9]{2}(?![\p{L}\p{N}])")]
    private static partial Regex YearPattern();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?<kind>cd|dvd(?![ -]?[59](?![0-9]))|disc|disk|part|pt)[ -]?(?<number>[0-9]{1,2})(?![\p{L}\p{N}])", RegexOptions.IgnoreCase)]
    private static partial Regex PartPattern();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?<kind>cd|dvd(?![ -]?[59](?![0-9]))|disc|disk)[ -]?(?<number>[0-9]{1,2})(?![\p{L}\p{N}])", RegexOptions.IgnoreCase)]
    private static partial Regex DiscPattern();

    // Words of a release, not of a title; "web", "cam" and "ts" are left out, as titles use them.
    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?:[0-9]{3,4}[pi]|4k|uhd|hdr(?:10)?|10bit|blu-?ray|brrip|bdrip|bdremux|remux|web-?dl|webrip|hdrip|dvdrip|dvdscr|dvd[ -]?[59]|hdtv|x26[45]|h ?26[45]|hevc|xvid|divx|aac|e?ac3|dts|ddp?5|atmos|truehd|proper|repack|extended|unrated|remastered|limited|imax|internal|multi|dual|directors? cut|theatrical cut)(?![\p{L}\p{N}])", RegexOptions.IgnoreCase)]
    private static partial Regex ReleasePattern();

    // Release tags anywhere: "[YTS.MX] Film". Parentheses only at the end, as "(500) Days of Summer" has some in its title.
    [GeneratedRegex(@"\[[^\]]*\]|\{[^}]*\}")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\([^()]*\)\s*$")]
    private static partial Regex TrailingParentheses();

    [GeneratedRegex(@"\{(?<name>[a-z]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Token();

    // A language with its region, after a separator: "Film.pt-BR", "Film es-419", "Film.2010-pt-BR".
    [GeneratedRegex(@"(?<=[._ -])[A-Za-z]{2,3}-(?:[A-Za-z]{2}|[A-Za-z]{4}|[0-9]{3})$")]
    private static partial Regex RegionTail();
}
