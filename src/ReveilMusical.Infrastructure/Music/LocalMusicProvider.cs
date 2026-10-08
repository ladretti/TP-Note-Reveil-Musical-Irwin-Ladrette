using System.Collections.Frozen;
using ReveilMusical.Domain;

namespace ReveilMusical.Infrastructure.Music;

internal sealed class LocalMusicProvider
{
    private static readonly Track Default = new("Mr. Blue Sky", "Electric Light Orchestra");

    private readonly FrozenDictionary<Weather, Track> _catalog = new Dictionary<Weather, Track>
    {
        [Weather.Sun] = new("Here Comes the Sun", "The Beatles"),
        [Weather.Rain] = new("Singin' in the Rain", "Gene Kelly"),
        [Weather.Snow] = new("Let It Snow! Let It Snow! Let It Snow!", "Dean Martin"),
        [Weather.Cloudy] = Default,
    }.ToFrozenDictionary();

    public Track For(Weather weather) => _catalog.GetValueOrDefault(weather, Default);
}
