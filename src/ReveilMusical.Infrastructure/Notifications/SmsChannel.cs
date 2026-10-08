using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications.Fakes;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class SmsChannel(ISmsGateway gateway) : INotificationChannel
{
    public ChannelType Type => Channels.Sms;

    public Task<bool> TrySendAsync(WakeUpMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(message.Contact.AddressFor(Type) is { } phone && gateway.Send(phone, message.Body));
}
