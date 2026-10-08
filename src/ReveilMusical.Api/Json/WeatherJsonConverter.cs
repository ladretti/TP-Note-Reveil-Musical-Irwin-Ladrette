using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReveilMusical.Domain;

namespace ReveilMusical.Api.Json;

internal sealed class WeatherJsonConverter : JsonConverter<Weather>
{
    private static readonly FrozenDictionary<string, Weather> ByName = new Dictionary<string, Weather>
    {
        ["SOLEIL"] = Weather.Sun,
        ["PLUIE"] = Weather.Rain,
        ["NEIGE"] = Weather.Snow,
        ["NUAGEUX"] = Weather.Cloudy,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public override Weather Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && ByName.TryGetValue(reader.GetString()!, out var weather)
            ? weather
            : throw new JsonException("weather must be one of SOLEIL, PLUIE, NEIGE, NUAGEUX.");

    public override void Write(Utf8JsonWriter writer, Weather value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ByName.First(entry => entry.Value == value).Key);
}
