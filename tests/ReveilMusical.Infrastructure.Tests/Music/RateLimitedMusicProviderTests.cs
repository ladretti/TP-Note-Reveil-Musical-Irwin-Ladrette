using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests.Music;

public sealed class RateLimitedMusicProviderTests : IDisposable
{
    private static readonly Track Imagine = new("Imagine", "John Lennon");
    private readonly IMusicProvider _inner = Substitute.For<IMusicProvider>();
    private readonly FixedWindowRateLimiter _limiter = new(new FixedWindowRateLimiterOptions
    {
        PermitLimit = 1,
        Window = TimeSpan.FromHours(1),
        QueueLimit = 0,
    });
    private readonly RateLimitedMusicProvider _sut;

    public RateLimitedMusicProviderTests()
    {
        _inner.Name.Returns("ITunes");
        _inner.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(Imagine);
        _sut = new RateLimitedMusicProvider(_inner, _limiter, NullLogger<RateLimitedMusicProvider>.Instance);
    }

    public void Dispose() => _limiter.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Forwards_calls_within_quota() =>
        (await _sut.FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct)).ShouldBe(Imagine);

    [Fact]
    public async Task Skips_immediately_once_quota_is_exhausted()
    {
        await _sut.FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct);

        var second = await _sut.FindAsync(new TrackQuery("Yesterday", Weather.Sun), Ct);

        second.ShouldBeNull();
        await _inner.Received(1).FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
        _sut.Name.ShouldBe("ITunes");
    }
}
