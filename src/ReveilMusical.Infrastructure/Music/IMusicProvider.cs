using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal interface IMusicProvider
{
    string Name { get; }

    /// <returns><c>null</c> when the provider has no match or is unavailable.</returns>
    Task<Track?> FindAsync(TrackQuery query, CancellationToken cancellationToken);
}
