using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using ReveilMusical.Infrastructure.Music;

namespace ReveilMusical.Infrastructure.Options;

internal sealed class MusicOptions
{
    public const string SectionName = "Music";

    [MinLength(1)]
    public IReadOnlyList<MusicProviderKind> Providers { get; set; } = [];

    [Range(typeof(TimeSpan), "00:00:01", "7.00:00:00")]
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(24);

    [ValidateObjectMembers]
    public RemoteProviderOptions ITunes { get; set; } = new();

    [ValidateObjectMembers]
    public RemoteProviderOptions MusicBrainz { get; set; } = new();
}

internal sealed class RemoteProviderOptions
{
    [Required]
    public Uri? BaseUrl { get; set; }

    [Range(typeof(TimeSpan), "00:00:00.100", "00:00:30")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    [Range(1, 1000)]
    public int PermitsPerWindow { get; set; } = 1;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(1);

    [Required]
    public string UserAgent { get; set; } = string.Empty;
}
