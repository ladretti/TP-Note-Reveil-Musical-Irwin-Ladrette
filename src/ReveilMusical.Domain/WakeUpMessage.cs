namespace ReveilMusical.Domain;

public sealed record WakeUpMessage(string UserId, UserContact Contact, string Subject, string Body)
{
    // Spelled out rather than read from CultureInfo("fr-FR"): no dependency on ICU being installed.
    private static readonly string[] FrenchDays = ["dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi"];

    public static WakeUpMessage For(string userId, UserContact contact, DayOfWeek day, Weather weather, Track track)
    {
        var body = $"Bon {FrenchDays[(int)day]} {Describe(weather)} ! C'est l'heure de « {track.Title} » par {track.Artist}.";
        return new WakeUpMessage(userId, contact, "Réveil musical", track.ListenUrl is { } url ? $"{body} {url}" : body);
    }

    private static string Describe(Weather weather) => weather switch
    {
        Weather.Sun => "ensoleillé",
        Weather.Rain => "pluvieux",
        Weather.Snow => "neigeux",
        Weather.Cloudy => "nuageux",
        _ => throw new ArgumentOutOfRangeException(nameof(weather), weather, null),
    };
}
