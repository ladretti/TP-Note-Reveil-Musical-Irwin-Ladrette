using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Notifications;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Tests.Notifications;

public sealed class FallbackNotifierTests
{
    private static readonly WakeUpMessage Message = new("42", UserContact.None, "Réveil musical", "Debout !");

    private readonly INotificationChannel _push = ChannelOf(ChannelType.Push);
    private readonly INotificationChannel _sms = ChannelOf(ChannelType.Sms);
    private readonly INotificationChannel _email = ChannelOf(ChannelType.Email);

    private static INotificationChannel ChannelOf(ChannelType type)
    {
        var channel = Substitute.For<INotificationChannel>();
        channel.Type.Returns(type);
        channel.TrySendAsync(Arg.Any<WakeUpMessage>(), Arg.Any<CancellationToken>()).Returns(true);
        return channel;
    }

    private static FallbackNotifier CreateSut(params INotificationChannel[] channels) => new(
        channels,
        Microsoft.Extensions.Options.Options.Create(new NotificationOptions { FallbackOrder = [ChannelType.Push, ChannelType.Sms, ChannelType.Email] }),
        NullLogger<FallbackNotifier>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Uses_the_preferred_channel_first()
    {
        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, ChannelType.Email, Ct);

        result.ShouldBe(new NotificationResult(ChannelType.Email, UsedFallback: false));
        await _push.DidNotReceiveWithAnyArgs().TrySendAsync(default!, Ct);
    }

    [Fact]
    public async Task Falls_back_in_configured_order_without_retrying_the_preferred_channel()
    {
        _sms.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(false);
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, ChannelType.Sms, Ct);

        result.ShouldBe(new NotificationResult(ChannelType.Email, UsedFallback: true));
        await _sms.Received(1).TrySendAsync(Message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_when_a_channel_throws()
    {
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("push down"));

        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, ChannelType.Push, Ct);

        result.ShouldBe(new NotificationResult(ChannelType.Sms, UsedFallback: true));
    }

    [Fact]
    public async Task Without_preference_starts_with_configured_order()
    {
        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, preferred: null, Ct);

        result.ShouldBe(new NotificationResult(ChannelType.Push, UsedFallback: true));
    }

    [Fact]
    public async Task Skips_a_preferred_channel_that_is_not_registered()
    {
        var result = await CreateSut(_sms).NotifyAsync(Message, ChannelType.Push, Ct);

        result.ShouldBe(new NotificationResult(ChannelType.Sms, UsedFallback: true));
    }

    [Fact]
    public async Task Reports_not_delivered_when_every_channel_fails()
    {
        foreach (var channel in new[] { _push, _sms, _email })
        {
            channel.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(false);
        }

        (await CreateSut(_push, _sms, _email).NotifyAsync(Message, ChannelType.Push, Ct)).ShouldBe(NotificationResult.NotDelivered);
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => CreateSut(_push).NotifyAsync(Message, ChannelType.Push, cancellation.Token));
    }
}
