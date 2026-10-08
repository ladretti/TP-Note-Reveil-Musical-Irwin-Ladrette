using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class RegisteredChannelsValidator(IEnumerable<INotificationChannel> channels) : IValidateOptions<NotificationOptions>
{
    private readonly HashSet<ChannelType> _registered = [.. channels.Select(channel => channel.Type)];

    public ValidateOptionsResult Validate(string? name, NotificationOptions options)
    {
        var unknown = options.FallbackOrder.Where(channel => string.IsNullOrWhiteSpace(channel) || !_registered.Contains(new ChannelType(channel))).ToList();
        return unknown.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Notifications:FallbackOrder names unregistered channels: {string.Join(", ", unknown)}. Registered: {string.Join(", ", _registered)}.");
    }
}
