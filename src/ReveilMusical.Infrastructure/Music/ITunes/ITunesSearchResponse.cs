namespace ReveilMusical.Infrastructure.Music.ITunes;

internal sealed record ITunesSearchResponse(IReadOnlyList<ITunesTrack>? Results);

internal sealed record ITunesTrack(string? TrackName, string? ArtistName, Uri? TrackViewUrl);
