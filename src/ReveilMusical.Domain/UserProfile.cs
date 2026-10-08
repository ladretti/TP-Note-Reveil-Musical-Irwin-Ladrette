namespace ReveilMusical.Domain;

public sealed record UserProfile(
    string UserId,
    IReadOnlyDictionary<SongSlot, string> Songs,
    string FallbackSong,
    ChannelType PreferredChannel,
    UserContact Contact)
{
    public string SongFor(DayOfWeek day, Weather weather) =>
        Songs.TryGetValue(new SongSlot(day, weather), out var song) ? song : FallbackSong;
}
