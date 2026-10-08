using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Notifications;

internal static class Channels
{
    public static readonly ChannelType Email = new("Email");
    public static readonly ChannelType Sms = new("Sms");
    public static readonly ChannelType Push = new("Push");
}
