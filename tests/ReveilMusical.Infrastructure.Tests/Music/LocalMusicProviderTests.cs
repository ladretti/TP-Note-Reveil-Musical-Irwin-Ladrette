using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests.Music;

public sealed class LocalMusicProviderTests
{
    public static TheoryData<Weather> AllWeathers => [.. Enum.GetValues<Weather>()];

    [Theory]
    [MemberData(nameof(AllWeathers))]
    public void Always_has_a_track_for_every_weather(Weather weather)
    {
        var track = new LocalMusicProvider().For(weather);

        track.Title.ShouldNotBeNullOrWhiteSpace();
        track.Artist.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Never_fails_even_on_an_undefined_weather() =>
        new LocalMusicProvider().For((Weather)42).ShouldNotBeNull();
}
