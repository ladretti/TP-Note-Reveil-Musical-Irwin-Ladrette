using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReveilMusical.Api.Json;

internal abstract class FrenchNameJsonConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    private readonly FrozenDictionary<string, T> _byName;
    private readonly string _error;

    protected FrenchNameJsonConverter(string field, IReadOnlyDictionary<string, T> names)
    {
        _byName = names.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _error = $"{field} must be one of {string.Join(", ", names.Keys)}.";
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && _byName.TryGetValue(reader.GetString()!, out var value)
            ? value
            : throw new JsonException(_error);

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(_byName.First(entry => EqualityComparer<T>.Default.Equals(entry.Value, value)).Key);
}
