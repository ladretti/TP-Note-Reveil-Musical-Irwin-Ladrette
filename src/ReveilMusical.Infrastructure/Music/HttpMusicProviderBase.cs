using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal abstract class HttpMusicProviderBase<TResponse>(HttpClient httpClient, ILogger logger) : IMusicProvider
{
    public abstract string Name { get; }

    public async Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Title);

        try
        {
            using var response = await httpClient.GetAsync(BuildRequestUri(query.Title), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("{Provider} answered HTTP {StatusCode}", Name, (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<TResponse>(JsonSerializerOptions.Web, cancellationToken);
            return payload is null ? null : Map(payload);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "{Provider} unavailable", Name);
            return null;
        }
    }

    protected abstract Uri BuildRequestUri(string title);

    protected abstract Track? Map(TResponse response);
}
