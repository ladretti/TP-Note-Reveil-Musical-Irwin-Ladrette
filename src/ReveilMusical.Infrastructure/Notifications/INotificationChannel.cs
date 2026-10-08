using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Notifications;

internal interface INotificationChannel
{
    ChannelType Type { get; }

    /// <returns><c>false</c> when the channel cannot reach the user (missing contact, refusal).</returns>
    Task<bool> TrySendAsync(WakeUpMessage message, CancellationToken cancellationToken);
}
