using System.ComponentModel.DataAnnotations;

namespace ReveilMusical.Infrastructure.Options;

internal sealed class ProfileOptions
{
    public const string SectionName = "Profiles";

    [Range(typeof(TimeSpan), "00:00:00.100", "00:00:30")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
}
