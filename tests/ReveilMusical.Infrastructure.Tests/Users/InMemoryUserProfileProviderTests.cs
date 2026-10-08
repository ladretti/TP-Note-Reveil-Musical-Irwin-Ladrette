using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Users;

namespace ReveilMusical.Infrastructure.Tests.Users;

public sealed class InMemoryUserProfileProviderTests
{
    private readonly InMemoryUserProfileProvider _sut = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Returns_known_profiles()
    {
        var profile = await _sut.GetAsync("42", Ct);

        profile.ShouldNotBeNull();
        profile.SongFor(DayOfWeek.Monday, Weather.Rain).ShouldBe("Riders on the Storm");
        profile.PreferredChannel.ShouldBe(ChannelType.Push);
    }

    [Fact]
    public async Task Returns_null_for_unknown_users() => (await _sut.GetAsync("unknown", Ct)).ShouldBeNull();
}
