using System.Text.Json.Serialization;
using ReveilMusical.Api.Json;
using ReveilMusical.Domain;

namespace ReveilMusical.Api;

internal sealed record WakeUpRequestDto(
    string UserId,
    [property: JsonConverter(typeof(DayOfWeekJsonConverter))] DayOfWeek Day,
    [property: JsonConverter(typeof(WeatherJsonConverter))] Weather Weather);
