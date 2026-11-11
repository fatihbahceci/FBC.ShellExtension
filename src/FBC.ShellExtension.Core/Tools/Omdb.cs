using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>Why OMDb gave no answer to go on with.</summary>
public enum OmdbFailure
{
    /// <summary>OMDb does not take the key, or none was given.</summary>
    InvalidKey,

    /// <summary>The key's daily searches are used up.</summary>
    LimitReached,

    /// <summary>Anything else: an error OMDb named, an answer that is not OMDb's, no answer at all.</summary>
    Other,
}

/// <summary>OMDb gave no answer to go on with; the search stops, as the next one would fare no better.</summary>
public sealed class OmdbException(OmdbFailure failure, string message) : Exception(message)
{
    public OmdbFailure Failure { get; } = failure;
}

/// <summary>A lookup's outcome: the film when it is certain, otherwise the films to choose from (none when nothing was found).</summary>
public sealed record MovieLookup(MovieInfo? Match, IReadOnlyList<MovieInfo> Candidates);

/// <summary>
/// The films in an OMDb answer. <paramref name="More"/>: OMDb has more than it lists ("Too many
/// results."), so not even one of them is the only one.
/// </summary>
public sealed record OmdbFilms(IReadOnlyList<MovieInfo> Films, bool More = false)
{
    public static OmdbFilms None { get; } = new([]);
}

/// <summary>
/// The OMDb API (https://www.omdbapi.com/, ADR-0046): its addresses, its answers and the order
/// a film is looked for in. The request itself is the caller's: network code stays out of Core.
/// The key goes in the address, the only way OMDb takes it; the address is never shown or logged.
/// </summary>
public static partial class Omdb
{
    public const string ApiKeyPage = "https://www.omdbapi.com/apikey.aspx";

    private const string BaseUrl = "https://www.omdbapi.com/";

    /// <summary>A film every key finds, for checking a key: The Shawshank Redemption.</summary>
    public const string CheckId = "tt0111161";

    /// <summary>Films whose title holds the words; ten at most.</summary>
    public static Uri SearchUri(string apiKey, string query, string? year) => Address(apiKey, "s", query, year);

    /// <summary>The film of that title, the best known one when several have it.</summary>
    public static Uri TitleUri(string apiKey, string title, string? year) => Address(apiKey, "t", title, year);

    public static Uri IdUri(string apiKey, string imdbId) => Address(apiKey, "i", imdbId, null);

    private static Uri Address(string apiKey, string field, string value, string? year) =>
        new($"{BaseUrl}?apikey={Uri.EscapeDataString(apiKey.Trim())}&type=movie&{field}={Uri.EscapeDataString(value)}"
            + (string.IsNullOrEmpty(year) ? "" : $"&y={Uri.EscapeDataString(year)}"));

    /// <summary>The IMDb id in a text: <c>tt0111161</c> as it is, or in an address such as <c>https://www.imdb.com/title/tt0111161/</c>.</summary>
    public static string? ImdbIdIn(string text) => ImdbId().Match(text) is { Success: true } match ? match.Value.ToLowerInvariant() : null;

    /// <summary>The films in OMDb's answer; none when it found nothing.</summary>
    /// <param name="status">The HTTP status. OMDb answers a bad key or a used-up limit with 401 and says which in the body.</param>
    /// <exception cref="OmdbException">The key, the limit, another error OMDb named, or an answer that is not OMDb's.</exception>
    public static OmdbFilms Read(int status, string body)
    {
        OmdbAnswer? answer;
        try
        {
            answer = JsonSerializer.Deserialize(body, OmdbJsonContext.Default.OmdbAnswer);
        }
        catch (JsonException)
        {
            answer = null;
        }

        bool ok = status is >= 200 and < 300;
        if (answer?.Response is not ("True" or "False"))
        {
            throw new OmdbException(OmdbFailure.Other, ok ? Strings.ImdbAnswerUnreadable() : Strings.ImdbHttpError(status));
        }

        if (answer.Response == "False")
        {
            return answer.Error switch
            {
                "Movie not found!" or "Incorrect IMDb ID." or "Series or episode not found!" => OmdbFilms.None,
                "Too many results." => new OmdbFilms([], More: true),
                "Invalid API key!" or "No API key provided." => throw new OmdbException(OmdbFailure.InvalidKey, Strings.ImdbKeyInvalid()),
                "Request limit reached!" => throw new OmdbException(OmdbFailure.LimitReached, Strings.ImdbLimitReached()),
                _ => throw new OmdbException(OmdbFailure.Other, Strings.ImdbAnswerError(answer.Error ?? Strings.ImdbHttpError(status))),
            };
        }

        IEnumerable<OmdbFilm> films = answer.Search ?? (answer.ImdbId is null ? [] : [answer]);
        return new OmdbFilms([.. films.Where(film => !string.IsNullOrEmpty(film.ImdbId) && !string.IsNullOrEmpty(film.Title))
            .Select(film => new MovieInfo(film.Title!, film.Year ?? "", film.ImdbId!))
            .DistinctBy(film => film.ImdbId, StringComparer.OrdinalIgnoreCase)]);
    }

    /// <summary>
    /// Looks the film up. With a year, the title and year are taken when OMDb has exactly that
    /// film, or the year's search has one result; several are offered; none, and the films of
    /// the title in any year are offered, not taken: the name's year may be a release's (when
    /// that search lists none either, the best known film of the title is offered). Without
    /// a year, a film is taken only when the search has just that one (and the title answer, if
    /// any, is the same): the title answer alone is only the best known of the name (G-0120).
    /// "Too many results." is never one result.
    /// </summary>
    /// <param name="fetch">Asks OMDb an address and reads its answer (<see cref="Read"/>).</param>
    public static async Task<MovieLookup> LookupAsync(MovieGuess guess, string apiKey,
        Func<Uri, CancellationToken, Task<OmdbFilms>> fetch, CancellationToken cancellation)
    {
        if (guess.Year is { } year)
        {
            OmdbFilms exact = await fetch(TitleUri(apiKey, guess.Title, year), cancellation).ConfigureAwait(false);
            if (exact.Films.Count == 1)
            {
                return new MovieLookup(exact.Films[0], []);
            }

            OmdbFilms ofYear = await fetch(SearchUri(apiKey, guess.Title, year), cancellation).ConfigureAwait(false);
            if (ofYear.Films.Count == 1 && !ofYear.More)
            {
                return new MovieLookup(ofYear.Films[0], []);
            }

            if (ofYear.Films.Count > 1)
            {
                return new MovieLookup(null, ofYear.Films);
            }

            OmdbFilms anyYear = await fetch(SearchUri(apiKey, guess.Title, null), cancellation).ConfigureAwait(false);
            if (anyYear.Films.Count > 0)
            {
                return new MovieLookup(null, anyYear.Films);
            }

            // Nothing listed (none, or too many to list): the best known film of the title is offered.
            return new MovieLookup(null, (await fetch(TitleUri(apiKey, guess.Title, null), cancellation).ConfigureAwait(false)).Films);
        }

        OmdbFilms titled = await fetch(TitleUri(apiKey, guess.Title, null), cancellation).ConfigureAwait(false);
        OmdbFilms found = await fetch(SearchUri(apiKey, guess.Title, null), cancellation).ConfigureAwait(false);
        if (found.Films.Count == 1 && !found.More
            && (titled.Films.Count == 0 || string.Equals(found.Films[0].ImdbId, titled.Films[0].ImdbId, StringComparison.OrdinalIgnoreCase)))
        {
            return new MovieLookup(found.Films[0], []);
        }

        // The best known film of the title first.
        return new MovieLookup(null, [.. titled.Films, .. found.Films.Where(film =>
            !titled.Films.Any(first => string.Equals(first.ImdbId, film.ImdbId, StringComparison.OrdinalIgnoreCase)))]);
    }

    [GeneratedRegex(@"(?<![a-z0-9])tt\d{7,10}(?![0-9])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImdbId();
}

/// <summary>An OMDb film: a search result, or a title or id answer itself.</summary>
internal class OmdbFilm
{
    [JsonPropertyName("Title")]
    public string? Title { get; set; }

    [JsonPropertyName("Year")]
    public string? Year { get; set; }

    [JsonPropertyName("imdbID")]
    public string? ImdbId { get; set; }
}

/// <summary>OMDb's answer: <c>Response</c> "True" with a film or a <c>Search</c> list, or "False" with an <c>Error</c>.</summary>
internal sealed class OmdbAnswer : OmdbFilm
{
    [JsonPropertyName("Response")]
    public string? Response { get; set; }

    [JsonPropertyName("Error")]
    public string? Error { get; set; }

    [JsonPropertyName("Search")]
    public List<OmdbFilm>? Search { get; set; }
}

// OMDb sends much more than is read here; what is not known is skipped.
[JsonSerializable(typeof(OmdbAnswer))]
internal sealed partial class OmdbJsonContext : JsonSerializerContext
{
}
