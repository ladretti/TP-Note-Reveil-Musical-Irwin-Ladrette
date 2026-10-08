using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;

namespace ReveilMusical.Api.Tests;

public sealed class WakeUpEndpointTests : IDisposable
{
    private static readonly UserProfile Alice = new(
        "42", new Dictionary<SongSlot, string>(), "Imagine", ChannelType.Sms, new UserContact(Phone: "+33600000042"));

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public WakeUpEndpointTests()
    {
        _client = _factory.CreateClient();
        _factory.Profiles.GetAsync("42", Arg.Any<CancellationToken>()).Returns(Alice);
        _factory.Tracks.ResolveAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedTrack(new Track("Imagine", "John Lennon", new Uri("https://music.example/imagine")), IsFallback: false));
        _factory.Notifier.NotifyAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ChannelType?>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(ChannelType.Sms, UsedFallback: false));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> PostAsync(string json) =>
        _client.PostAsync(new Uri("/wakeups", UriKind.Relative), new StringContent(json, Encoding.UTF8, "application/json"), Ct);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement;

    [Fact]
    public async Task Returns_200_with_the_track_and_the_channel_used()
    {
        using var response = await PostAsync("""{ "userId": "42", "day": "Monday", "weather": "PLUIE" }""");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("track").GetProperty("title").GetString().ShouldBe("Imagine");
        body.GetProperty("track").GetProperty("artist").GetString().ShouldBe("John Lennon");
        body.GetProperty("track").GetProperty("listenUrl").GetString().ShouldBe("https://music.example/imagine");
        body.GetProperty("deliveredVia").GetString().ShouldBe("Sms");
        body.GetProperty("degraded").GetBoolean().ShouldBeFalse();
        body.GetProperty("reasons").GetArrayLength().ShouldBe(0);
        await _factory.Tracks.Received(1).ResolveAsync(Arg.Is(new TrackQuery("Imagine", Weather.Rain)), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("""{ "userId": "42", "day": "Monday", "weather": 2 }""")]
    [InlineData("""{ "userId": "42", "day": 1, "weather": "PLUIE" }""")]
    [InlineData("""{ "userId": "42", "day": "Funday", "weather": "PLUIE" }""")]
    [InlineData("""{ "userId": "42", "day": "Monday", "weather": "RAIN" }""")]
    [InlineData("""{ "day": "Monday", "weather": "PLUIE" }""")]
    [InlineData("""{ "userId": null, "day": "Monday", "weather": "PLUIE" }""")]
    [InlineData("""{ "userId": "   ", "day": "Monday", "weather": "PLUIE" }""")]
    [InlineData("not json")]
    public async Task Invalid_payloads_return_400(string json)
    {
        using var response = await PostAsync(json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        await _factory.Tracks.DidNotReceiveWithAnyArgs().ResolveAsync(default!, Ct);
    }

    [Fact]
    public async Task Returns_404_for_unknown_users()
    {
        using var response = await PostAsync("""{ "userId": "unknown", "day": "Monday", "weather": "SOLEIL" }""");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Returns_503_when_no_channel_could_deliver()
    {
        _factory.Notifier.NotifyAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ChannelType?>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.NotDelivered);

        using var response = await PostAsync("""{ "userId": "42", "day": "Monday", "weather": "NEIGE" }""");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await ReadJsonAsync(response);
        body.GetProperty("degraded").GetBoolean().ShouldBeTrue();
        body.GetProperty("reasons")[0].GetString().ShouldBe("NotDelivered");
    }

    [Fact]
    public async Task Returns_500_problem_details_without_leaking_internals()
    {
        _factory.Notifier.NotifyAsync(Arg.Any<WakeUpMessage>(), Arg.Any<ChannelType?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("secret internal detail"));

        using var response = await PostAsync("""{ "userId": "42", "day": "Monday", "weather": "NUAGEUX" }""");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync(Ct)).ShouldNotContain("secret internal detail");
    }
}
