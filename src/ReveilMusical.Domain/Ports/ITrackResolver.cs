namespace ReveilMusical.Domain.Ports;

public interface ITrackResolver
{
    Task<ResolvedTrack> ResolveAsync(TrackQuery query, CancellationToken cancellationToken);
}

public sealed record ResolvedTrack(Track Track, bool IsFallback);
