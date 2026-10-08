using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Notifications;

internal sealed class RegisteredChannelsValidator(IEnumerable<INotificationChannel> channels) : IValidateOptions<NotificationOptions>
{
    private readonly ChannelType[] _declared = [.. channels.Select(channel => channel.Type)];

    public ValidateOptionsResult Validate(string? name, NotificationOptions options)
    {
        var duplicates = _declared.GroupBy(type => type).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        if (duplicates.Count > 0)
        {
            return ValidateOptionsResult.Fail($"Several notification channels declare the same name: {string.Join(", ", duplicates)}.");
        }

        var registered = _declared.ToHashSet();
        var unknown = options.FallbackOrder.Where(channel => string.IsNullOrWhiteSpace(channel) || !registered.Contains(new ChannelType(channel))).ToList();
        return unknown.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Notifications:FallbackOrder names unregistered channels: {string.Join(", ", unknown)}. Registered: {string.Join(", ", registered)}.");
    }
}
