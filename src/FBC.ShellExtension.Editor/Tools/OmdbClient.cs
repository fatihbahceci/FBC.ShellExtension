using System.Text;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Asks OMDb (ADR-0046); what to ask and how to read the answer is Core's <see cref="Omdb"/>.
/// The address carries the API key, so it is never shown: what failed is told in OMDb's or the
/// network's words, which do not hold the query.
/// </summary>
internal static class OmdbClient
{
    private static readonly HttpClient s_http = Create();

    private static HttpClient Create()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{ProductInfo.Name}/{ProductInfo.Version}");
        return http;
    }

    /// <exception cref="OmdbException">OMDb refused, or did not answer.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellation"/> was cancelled.</exception>
    public static async Task<OmdbFilms> FetchAsync(Uri address, CancellationToken cancellation)
    {
        try
        {
            using HttpResponseMessage response = await s_http.GetAsync(address, cancellation);

            // As bytes, in OMDb's UTF-8: a charset .NET does not know (a proxy's page) would
            // otherwise throw while the body is read, instead of reading as an answer that is not OMDb's.
            byte[] body = await response.Content.ReadAsByteArrayAsync(cancellation);
            return Omdb.Read((int)response.StatusCode, Encoding.UTF8.GetString(body));
        }
        catch (HttpRequestException ex)
        {
            throw new OmdbException(OmdbFailure.Other, Strings.ImdbNoAnswer(ex.Message));
        }
        catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
        {
            // The timeout, not the user.
            throw new OmdbException(OmdbFailure.Other, Strings.ImdbNoAnswer(ex.Message));
        }
    }

    /// <summary><see cref="Omdb.LookupAsync"/> through this client.</summary>
    public static Task<MovieLookup> LookupAsync(MovieGuess guess, string apiKey, CancellationToken cancellation) =>
        Omdb.LookupAsync(guess, apiKey, FetchAsync, cancellation);
}
