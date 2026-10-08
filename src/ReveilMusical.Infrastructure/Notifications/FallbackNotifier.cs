using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class FallbackNotifier(
    IEnumerable<INotificationChannel> channels,
    IOptions<NotificationOptions> options,
    ILogger<FallbackNotifier> logger) : IWakeUpNotifier
{
    private readonly Dictionary<ChannelType, INotificationChannel> _channels = channels.ToDictionary(channel => channel.Type);

    public async Task<NotificationResult> NotifyAsync(WakeUpMessage message, ChannelType? preferred, CancellationToken cancellationToken)
    {
        foreach (var type in AttemptOrder(preferred))
        {
            if (await TrySendAsync(type, message, cancellationToken))
            {
                return new NotificationResult(type, UsedFallback: type != preferred);
            }
        }

        return NotificationResult.NotDelivered;
    }

    private IEnumerable<ChannelType> AttemptOrder(ChannelType? preferred)
    {
        var configured = options.Value.FallbackOrder.Select(name => new ChannelType(name));
        return (preferred is { } first ? configured.Prepend(first) : configured).Distinct();
    }

    private async Task<bool> TrySendAsync(ChannelType type, WakeUpMessage message, CancellationToken cancellationToken)
    {
        if (!_channels.TryGetValue(type, out var channel))
        {
            logger.LogWarning("No {Channel} channel registered", type);
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.ChannelTimeout);
        try
        {
            if (await channel.TrySendAsync(message, timeout.Token))
            {
                return true;
            }

            logger.LogWarning("{Channel} could not reach user {UserId}", type, message.UserId);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "{Channel} failed for user {UserId}", type, message.UserId);
        }

        return false;
    }
}
