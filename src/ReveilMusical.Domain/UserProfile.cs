namespace ReveilMusical.Domain;

public sealed record UserProfile(
    string UserId,
    IReadOnlyDictionary<SongSlot, string> Songs,
    string FallbackSong,
    ChannelType PreferredChannel,
    UserContact Contact)
{
    /// <summary>The profile service was unreachable: this is the last copy seen, possibly outdated.</summary>
    public bool IsStale { get; init; }

    public string SongFor(DayOfWeek day, Weather weather) =>
        Songs.TryGetValue(new SongSlot(day, weather), out var song) ? song : FallbackSong;
}
