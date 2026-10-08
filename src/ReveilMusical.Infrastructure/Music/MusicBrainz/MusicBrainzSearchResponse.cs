using System.Text.Json.Serialization;

namespace ReveilMusical.Infrastructure.Music.MusicBrainz;

internal sealed record MusicBrainzSearchResponse(IReadOnlyList<MusicBrainzRecording>? Recordings);

internal sealed record MusicBrainzRecording(
    string? Title,
    [property: JsonPropertyName("artist-credit")] IReadOnlyList<MusicBrainzArtistCredit>? ArtistCredit);

internal sealed record MusicBrainzArtistCredit(string? Name);
