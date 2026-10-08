using System.Text.Json.Serialization;

namespace ReveilMusical.Api.Json;

internal sealed class StrictEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
