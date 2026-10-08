using System.ComponentModel.DataAnnotations;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Options;

internal sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    [MinLength(1)]
    public IReadOnlyList<ChannelType> FallbackOrder { get; set; } = [];
}
