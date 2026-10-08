using ReveilMusical.Application;

namespace ReveilMusical.Api;

internal sealed record WakeUpResponse(TrackResponse? Track, string? DeliveredVia, bool Degraded, IReadOnlyList<string> Reasons)
{
    public static WakeUpResponse From(WakeUpResult result) => new(
        result.Track is { } track ? new TrackResponse(track.Title, track.Artist, track.ListenUrl) : null,
        result.DeliveredVia?.ToString(),
        result.IsDegraded,
        [.. result.Reasons.Select(reason => reason.ToString())]);
}

internal sealed record TrackResponse(string Title, string Artist, Uri? ListenUrl);
