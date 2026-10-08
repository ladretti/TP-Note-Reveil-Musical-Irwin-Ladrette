using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class RateLimitedMusicProvider(IMusicProvider inner, RateLimiter limiter, ILogger<RateLimitedMusicProvider> logger) : IMusicProvider
{
    public string Name => inner.Name;

    public async Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        // Waiting for a permit would delay the wake-up: the next provider is a better bet.
        using var lease = limiter.AttemptAcquire();
        if (!lease.IsAcquired)
        {
            logger.LogWarning("{Provider} quota reached, skipping", Name);
            return null;
        }

        return await inner.FindAsync(query, cancellationToken);
    }
}
