namespace ReveilMusical.Domain.Ports;

public interface IWakeUpNotifier
{
    Task<NotificationResult> NotifyAsync(WakeUpMessage message, ChannelType? preferred, CancellationToken cancellationToken);
}

public sealed record NotificationResult(ChannelType? DeliveredVia, bool UsedFallback)
{
    public static NotificationResult NotDelivered { get; } = new(null, UsedFallback: true);

    public bool Delivered => DeliveredVia is not null;
}
