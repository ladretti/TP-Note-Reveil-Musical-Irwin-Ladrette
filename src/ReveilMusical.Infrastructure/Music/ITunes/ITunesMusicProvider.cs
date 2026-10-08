using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music.ITunes;

internal sealed class ITunesMusicProvider(HttpClient httpClient, ILogger<ITunesMusicProvider> logger)
    : HttpMusicProviderBase<ITunesSearchResponse>(httpClient, logger)
{
    public override string Name => nameof(MusicProviderKind.ITunes);

    protected override Uri BuildRequestUri(string title) =>
        new($"search?term={Uri.EscapeDataString(title)}&media=music&limit=5", UriKind.Relative);

    protected override Track? Map(ITunesSearchResponse response) =>
        (response.Results ?? [])
            .Where(result => !string.IsNullOrWhiteSpace(result.TrackName) && !string.IsNullOrWhiteSpace(result.ArtistName))
            .Select(result => new Track(result.TrackName!, result.ArtistName!, result.TrackViewUrl))
            .FirstOrDefault();
}
