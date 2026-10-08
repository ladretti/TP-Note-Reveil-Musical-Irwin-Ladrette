using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class FallbackTrackResolver(
    IReadOnlyList<IMusicProvider> providers,
    LocalMusicProvider local,
    ILogger<FallbackTrackResolver> logger) : ITrackResolver
{
    public async Task<ResolvedTrack> ResolveAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            for (var rank = 0; rank < providers.Count; rank++)
            {
                var track = await TryFindAsync(providers[rank], query, cancellationToken);
                if (track is not null)
                {
                    return new ResolvedTrack(track, IsFallback: rank > 0);
                }
            }
        }

        logger.LogWarning("No music provider answered for {Title}, using local catalog", query.Title);
        return new ResolvedTrack(local.For(query.Weather), IsFallback: true);
    }

    private async Task<Track?> TryFindAsync(IMusicProvider provider, TrackQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return await provider.FindAsync(query, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "{Provider} failed unexpectedly", provider.Name);
            return null;
        }
    }
}
