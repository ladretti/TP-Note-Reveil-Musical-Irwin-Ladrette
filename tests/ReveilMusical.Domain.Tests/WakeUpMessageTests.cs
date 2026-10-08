using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public sealed class WakeUpMessageTests
{
    private static readonly UserContact Contact = new(Phone: "+33600000000");

    [Theory]
    [InlineData(DayOfWeek.Monday, Weather.Rain, "Bon lundi pluvieux !")]
    [InlineData(DayOfWeek.Saturday, Weather.Sun, "Bon samedi ensoleillé !")]
    [InlineData(DayOfWeek.Sunday, Weather.Snow, "Bon dimanche neigeux !")]
    [InlineData(DayOfWeek.Friday, Weather.Cloudy, "Bon vendredi nuageux !")]
    public void For_greets_with_the_day_and_the_weather(DayOfWeek day, Weather weather, string greeting) =>
        WakeUpMessage.For("42", Contact, day, weather, new Track("Imagine", "John Lennon")).Body.ShouldStartWith(greeting);

    [Fact]
    public void For_announces_the_track_and_keeps_recipient_details()
    {
        var message = WakeUpMessage.For("42", Contact, DayOfWeek.Monday, Weather.Rain, new Track("Imagine", "John Lennon"));

        message.ShouldBe(new WakeUpMessage(
            "42",
            Contact,
            "Réveil musical",
            "Bon lundi pluvieux ! C'est l'heure de « Imagine » par John Lennon."));
    }

    [Fact]
    public void For_appends_the_listen_link_when_known()
    {
        var track = new Track("Imagine", "John Lennon", new Uri("https://music.example/imagine"));

        WakeUpMessage.For("42", Contact, DayOfWeek.Monday, Weather.Rain, track).Body
            .ShouldEndWith("par John Lennon. https://music.example/imagine");
    }
}
