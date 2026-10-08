using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;

namespace ReveilMusical.Application.Tests;

public sealed class WakeUpServiceTests
{
    private static readonly Track Song = new("Riders on the Storm", "The Doors");
    private static readonly UserContact AliceContact = new(Phone: "+33600000001");
    private static readonly UserProfile Alice = new(
        "42",
        new Dictionary<SongSlot, string> { [new SongSlot(DayOfWeek.Monday, Weather.Rain)] = "Riders on the Storm" },
        "Wake Me Up",
        ChannelType.Sms,
        AliceContact);
    private static readonly WakeUpRequest MondayRain = new("42", DayOfWeek.Monday, Weather.Rain);

    private readonly IUserProfileProvider _profiles = Substitute.For<IUserProfileProvider>();
    private readonly ITrackResolver _tracks = Substitute.For<ITrackResolver>();
    private readonly IWakeUpNotifier _notifier = Substitute.For<IWakeUpNotifier>();
    private readonly WakeUpService _sut;

    public WakeUpServiceTests()
    {
        _sut = new WakeUpService(_profiles, _tracks, _notifier, NullLogger<WakeUpService>.Instance);
        _profiles.GetAsync("42", Arg.Any<CancellationToken>()).Returns(Alice);
        _tracks.ResolveAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(new ResolvedTrack(Song, IsFallback: false));
        _notifier.NotifyAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ChannelType?>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(ChannelType.Sms, UsedFallback: false));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Wakes_the_user_with_the_song_of_the_day_on_the_preferred_channel()
    {
        var result = await _sut.WakeUpAsync(MondayRain, Ct);

        result.ShouldBe(new WakeUpResult(WakeUpStatus.Delivered, Song, ChannelType.Sms, result.Reasons));
        result.IsDegraded.ShouldBeFalse();
        await _tracks.Received(1).ResolveAsync(Arg.Is(new TrackQuery("Riders on the Storm", Weather.Rain)), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyAsync(
            Arg.Is<WakeUpMessage>(m => m.UserId == "42" && m.Contact == AliceContact && m.Body.Contains("Riders on the Storm", StringComparison.Ordinal)),
            ChannelType.Sms,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_unknown_user_without_resolving_music()
    {
        var result = await _sut.WakeUpAsync(MondayRain with { UserId = "unknown" }, Ct);

        result.ShouldBe(WakeUpResult.UserNotFound);
        await _tracks.DidNotReceiveWithAnyArgs().ResolveAsync(default!, Ct);
    }

    [Fact]
    public async Task Still_wakes_the_user_when_the_profile_service_is_down()
    {
        _profiles.GetAsync("42", Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("down"));

        var result = await _sut.WakeUpAsync(MondayRain, Ct);

        result.Status.ShouldBe(WakeUpStatus.Delivered);
        result.Reasons.ShouldBe([DegradationReason.ProfileUnavailable]);
        await _tracks.Received(1).ResolveAsync(Arg.Is(new TrackQuery(null, Weather.Rain)), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyAsync(Arg.Is<WakeUpMessage>(m => m.Contact == UserContact.None), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Flags_music_fallback()
    {
        _tracks.ResolveAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(new ResolvedTrack(Song, IsFallback: true));

        var result = await _sut.WakeUpAsync(MondayRain, Ct);

        result.Reasons.ShouldBe([DegradationReason.MusicFallbackUsed]);
    }

    [Fact]
    public async Task Flags_channel_fallback()
    {
        _notifier.NotifyAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ChannelType?>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(ChannelType.Email, UsedFallback: true));

        var result = await _sut.WakeUpAsync(MondayRain, Ct);

        result.DeliveredVia.ShouldBe(ChannelType.Email);
        result.Reasons.ShouldBe([DegradationReason.ChannelFallbackUsed]);
    }

    [Fact]
    public async Task Reports_not_delivered_when_every_channel_failed()
    {
        _notifier.NotifyAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ChannelType?>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.NotDelivered);

        var result = await _sut.WakeUpAsync(MondayRain, Ct);

        result.ShouldBe(new WakeUpResult(WakeUpStatus.NotDelivered, Song, null, result.Reasons));
        result.Reasons.ShouldBe([DegradationReason.NotDelivered]);
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _profiles.GetAsync("42", Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => _sut.WakeUpAsync(MondayRain, cancellation.Token));
    }
}
