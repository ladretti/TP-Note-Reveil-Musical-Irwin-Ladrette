namespace ReveilMusical.Api.Json;

internal sealed class DayOfWeekJsonConverter() : FrenchNameJsonConverter<DayOfWeek>("day", new Dictionary<string, DayOfWeek>
{
    ["LUNDI"] = DayOfWeek.Monday,
    ["MARDI"] = DayOfWeek.Tuesday,
    ["MERCREDI"] = DayOfWeek.Wednesday,
    ["JEUDI"] = DayOfWeek.Thursday,
    ["VENDREDI"] = DayOfWeek.Friday,
    ["SAMEDI"] = DayOfWeek.Saturday,
    ["DIMANCHE"] = DayOfWeek.Sunday,
});
