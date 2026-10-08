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

    private readonly INotificationChannel _push = ChannelOf(Channels.Push);
    private readonly INotificationChannel _sms = ChannelOf(Channels.Sms);
    private readonly INotificationChannel _email = ChannelOf(Channels.Email);

    private static INotificationChannel ChannelOf(ChannelType type)
    {
        var channel = Substitute.For<INotificationChannel>();
        channel.Type.Returns(type);
        channel.TrySendAsync(Arg.Any<WakeUpMessage>(), Arg.Any<CancellationToken>()).Returns(true);
        return channel;
    }

    private static FallbackNotifier CreateSut(params INotificationChannel[] channels) => new(
        channels,
        Microsoft.Extensions.Options.Options.Create(new NotificationOptions { FallbackOrder = ["Push", "Sms", "Email"], ChannelTimeout = TimeSpan.FromMilliseconds(200) }),
        NullLogger<FallbackNotifier>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Uses_the_preferred_channel_first()
    {
        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, Channels.Email, Ct);

        result.ShouldBe(new NotificationResult(Channels.Email, UsedFallback: false));
        await _push.DidNotReceiveWithAnyArgs().TrySendAsync(default!, Ct);
    }

    [Fact]
    public async Task Falls_back_in_configured_order_without_retrying_the_preferred_channel()
    {
        _sms.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(false);
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, Channels.Sms, Ct);

        result.ShouldBe(new NotificationResult(Channels.Email, UsedFallback: true));
        await _sms.Received(1).TrySendAsync(Message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_when_a_channel_throws()
    {
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("push down"));

        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, Channels.Push, Ct);

        result.ShouldBe(new NotificationResult(Channels.Sms, UsedFallback: true));
    }

    [Fact]
    public async Task Without_preference_starts_with_configured_order()
    {
        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, preferred: null, Ct);

        result.ShouldBe(new NotificationResult(Channels.Push, UsedFallback: true));
    }

    [Fact]
    public async Task Skips_a_preferred_channel_that_is_not_registered()
    {
        var result = await CreateSut(_sms).NotifyAsync(Message, Channels.Push, Ct);

        result.ShouldBe(new NotificationResult(Channels.Sms, UsedFallback: true));
    }

    [Fact]
    public async Task Accepts_a_new_channel_without_any_domain_change()
    {
        var whatsApp = ChannelOf(new ChannelType("WhatsApp"));

        var result = await CreateSut(_push, whatsApp).NotifyAsync(Message, new ChannelType("whatsapp"), Ct);

        result.ShouldBe(new NotificationResult(new ChannelType("WhatsApp"), UsedFallback: false));
        await _push.DidNotReceiveWithAnyArgs().TrySendAsync(default!, Ct);
    }

    [Fact]
    public async Task Falls_back_when_a_channel_hangs()
    {
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
            return true;
        });

        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, Channels.Push, Ct);

        result.ShouldBe(new NotificationResult(Channels.Sms, UsedFallback: true));
    }

    [Fact]
    public async Task Falls_back_when_a_channel_blocks_and_ignores_cancellation()
    {
        using var release = new ManualResetEventSlim();
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            release.Wait(TimeSpan.FromSeconds(10), CancellationToken.None);
            return Task.FromResult(true);
        });

        var result = await CreateSut(_push, _sms, _email).NotifyAsync(Message, Channels.Push, Ct);

        result.ShouldBe(new NotificationResult(Channels.Sms, UsedFallback: true));
        release.Set();
    }

    [Fact]
    public async Task Reports_not_delivered_when_every_channel_fails()
    {
        foreach (var channel in new[] { _push, _sms, _email })
        {
            channel.TrySendAsync(Message, Arg.Any<CancellationToken>()).Returns(false);
        }

        (await CreateSut(_push, _sms, _email).NotifyAsync(Message, Channels.Push, Ct)).ShouldBe(NotificationResult.NotDelivered);
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _push.TrySendAsync(Message, Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => CreateSut(_push).NotifyAsync(Message, Channels.Push, cancellation.Token));
    }
}
