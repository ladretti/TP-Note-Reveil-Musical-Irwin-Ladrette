using System.ComponentModel.DataAnnotations;

namespace ReveilMusical.Infrastructure.Options;

internal sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    [MinLength(1)]
    public IReadOnlyList<string> FallbackOrder { get; set; } = [];

    [Range(typeof(TimeSpan), "00:00:00.100", "00:00:30")]
    public TimeSpan ChannelTimeout { get; set; } = TimeSpan.FromSeconds(2);
}
