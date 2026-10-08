using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications.Fakes;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class PushChannel(IPushService service) : INotificationChannel
{
    public ChannelType Type => Channels.Push;

    public async Task<bool> TrySendAsync(WakeUpMessage message, CancellationToken cancellationToken)
    {
        if (message.Contact.AddressFor(Type) is not { } token)
        {
            return false;
        }

        var receipt = await service.PushAsync(new PushPayload(token, message.Subject, message.Body));
        return receipt.Accepted;
    }
}
