using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Tests.Music;

public sealed class FallbackTrackResolverTests
{
    private static readonly Track FromPrimary = new("Imagine", "John Lennon");
    private static readonly Track FromSecondary = new("Imagine", "A Perfect Circle");
    private static readonly TrackQuery Query = new("Imagine", Weather.Snow);

    private readonly IMusicProvider _primary = Substitute.For<IMusicProvider>();
    private readonly IMusicProvider _secondary = Substitute.For<IMusicProvider>();
    private readonly LocalMusicProvider _local = new();
    private readonly FallbackTrackResolver _sut;

    public FallbackTrackResolverTests() =>
        _sut = new FallbackTrackResolver([_primary, _secondary], _local, NullLogger<FallbackTrackResolver>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Uses_the_first_provider_that_answers()
    {
        _primary.FindAsync(Query, Arg.Any<CancellationToken>()).Returns(FromPrimary);

        (await _sut.ResolveAsync(Query, Ct)).ShouldBe(new ResolvedTrack(FromPrimary, IsFallback: false));
        await _secondary.DidNotReceiveWithAnyArgs().FindAsync(default!, Ct);
    }

    [Fact]
    public async Task Falls_through_to_the_next_provider_on_miss()
    {
        _primary.FindAsync(Query, Arg.Any<CancellationToken>()).Returns((Track?)null);
        _secondary.FindAsync(Query, Arg.Any<CancellationToken>()).Returns(FromSecondary);

        (await _sut.ResolveAsync(Query, Ct)).ShouldBe(new ResolvedTrack(FromSecondary, IsFallback: true));
    }

    [Fact]
    public async Task Falls_through_to_the_next_provider_on_unexpected_failure()
    {
        _primary.FindAsync(Query, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("bug"));
        _secondary.FindAsync(Query, Arg.Any<CancellationToken>()).Returns(FromSecondary);

        (await _sut.ResolveAsync(Query, Ct)).ShouldBe(new ResolvedTrack(FromSecondary, IsFallback: true));
    }

    [Fact]
    public async Task Never_stays_silent_when_every_provider_is_down()
    {
        (await _sut.ResolveAsync(Query, Ct)).ShouldBe(new ResolvedTrack(_local.For(Weather.Snow), IsFallback: true));
    }

    [Fact]
    public async Task Goes_straight_to_local_catalog_without_a_title()
    {
        var resolved = await _sut.ResolveAsync(new TrackQuery(null, Weather.Sun), Ct);

        resolved.ShouldBe(new ResolvedTrack(_local.For(Weather.Sun), IsFallback: true));
        await _primary.DidNotReceiveWithAnyArgs().FindAsync(default!, Ct);
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _primary.FindAsync(Query, Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => _sut.ResolveAsync(Query, cancellation.Token));
    }
}
