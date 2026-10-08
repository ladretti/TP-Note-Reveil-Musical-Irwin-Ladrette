using System.ComponentModel.DataAnnotations;

namespace ReveilMusical.Infrastructure.Options;

internal sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    [MinLength(1)]
    public IReadOnlyList<string> FallbackOrder { get; set; } = [];
}
