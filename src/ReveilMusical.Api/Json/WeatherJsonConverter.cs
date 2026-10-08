using ReveilMusical.Domain;

namespace ReveilMusical.Api.Json;

internal sealed class WeatherJsonConverter() : FrenchNameJsonConverter<Weather>("weather", new Dictionary<string, Weather>
{
    ["SOLEIL"] = Weather.Sun,
    ["PLUIE"] = Weather.Rain,
    ["NEIGE"] = Weather.Snow,
    ["NUAGEUX"] = Weather.Cloudy,
});
