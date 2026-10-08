using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music.MusicBrainz;

internal sealed class MusicBrainzMusicProvider(HttpClient httpClient, ILogger<MusicBrainzMusicProvider> logger)
    : HttpMusicProviderBase<MusicBrainzSearchResponse>(httpClient, logger)
{
    public override string Name => nameof(MusicProviderKind.MusicBrainz);

    protected override Uri BuildRequestUri(string title) =>
        new($"ws/2/recording?query={Uri.EscapeDataString(title)}&fmt=json&limit=5", UriKind.Relative);

    protected override Track? Map(MusicBrainzSearchResponse response) =>
        (response.Recordings ?? [])
            .Select(recording => (recording.Title, Artist: recording.ArtistCredit is { Count: > 0 } credits ? credits[0].Name : null))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Title) && !string.IsNullOrWhiteSpace(candidate.Artist))
            .Select(candidate => new Track(candidate.Title!, candidate.Artist!))
            .FirstOrDefault();
}
