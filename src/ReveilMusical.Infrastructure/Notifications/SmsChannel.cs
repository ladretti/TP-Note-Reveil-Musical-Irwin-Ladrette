using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications.Fakes;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class SmsChannel(ISmsGateway gateway) : INotificationChannel
{
    public ChannelType Type => ChannelType.Sms;

    public Task<bool> TrySendAsync(WakeUpMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(message.Contact.Phone is { } phone && gateway.Send(phone, message.Body));
}
