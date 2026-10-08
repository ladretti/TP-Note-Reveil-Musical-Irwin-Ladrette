using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class CachingMusicProvider(IMusicProvider inner, IMemoryCache cache, IOptions<MusicOptions> options) : IMusicProvider
{
    public string Name => inner.Name;

    public async Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken)
    {
        var key = $"music:{Name}:{query.Title?.Trim().ToUpperInvariant()}";
        if (cache.TryGetValue(key, out Track? cached))
        {
            return cached;
        }

        var track = await inner.FindAsync(query, cancellationToken);
        if (track is not null)
        {
            cache.Set(key, track, options.Value.CacheTtl);
        }

        return track;
    }
}
