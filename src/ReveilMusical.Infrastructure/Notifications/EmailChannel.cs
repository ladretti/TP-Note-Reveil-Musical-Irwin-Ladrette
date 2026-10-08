using System.Net;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications.Fakes;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class EmailChannel(IEmailClient client) : INotificationChannel
{
    public ChannelType Type => Channels.Email;

    public async Task<bool> TrySendAsync(WakeUpMessage message, CancellationToken cancellationToken)
    {
        if (message.Contact.AddressFor(Type) is not { } address)
        {
            return false;
        }

        await client.SendMailAsync(address, message.Subject, $"<p>{WebUtility.HtmlEncode(message.Body)}</p>");
        return true;
    }
}
