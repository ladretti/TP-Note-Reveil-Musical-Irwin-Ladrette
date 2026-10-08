using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public sealed class UserProfileTests
{
    private static readonly UserProfile Profile = new(
        "42",
        new Dictionary<SongSlot, string> { [new SongSlot(DayOfWeek.Monday, Weather.Rain)] = "Riders on the Storm" },
        FallbackSong: "Wake Me Up",
        ChannelType.Sms,
        UserContact.None);

    [Fact]
    public void SongFor_returns_the_song_chosen_for_this_day_and_weather() =>
        Profile.SongFor(DayOfWeek.Monday, Weather.Rain).ShouldBe("Riders on the Storm");

    [Theory]
    [InlineData(DayOfWeek.Monday, Weather.Sun)]
    [InlineData(DayOfWeek.Tuesday, Weather.Rain)]
    public void SongFor_falls_back_when_the_slot_is_not_covered(DayOfWeek day, Weather weather) =>
        Profile.SongFor(day, weather).ShouldBe("Wake Me Up");
}
