using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music.ITunes;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ReveilMusical.Infrastructure.Tests.Music;

public sealed class ITunesMusicProviderTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ITunesMusicProvider CreateSut(TimeSpan? timeout = null) => new(
        new HttpClient { BaseAddress = new Uri(_server.Url!), Timeout = timeout ?? TimeSpan.FromSeconds(5) },
        NullLogger<ITunesMusicProvider>.Instance);

    private void GivenSearch(string term, object body) => _server
        .Given(Request.Create().WithPath("/search").WithParam("term", term).WithParam("media", "music").WithParam("limit", "5").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(body));

    [Fact]
    public async Task Maps_the_first_track_and_its_store_link_to_a_neutral_listen_url()
    {
        GivenSearch("Imagine", new
        {
            resultCount = 2,
            results = new[]
            {
                new { trackName = "Imagine", artistName = "John Lennon", trackViewUrl = "https://music.apple.com/imagine" },
                new { trackName = "Imagine (Live)", artistName = "John Lennon", trackViewUrl = "https://music.apple.com/live" },
            },
        });

        var track = await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct);

        track.ShouldBe(new Track("Imagine", "John Lennon", new Uri("https://music.apple.com/imagine")));
    }

    [Fact]
    public async Task Skips_incomplete_results()
    {
        GivenSearch("Imagine", new
        {
            results = new object[]
            {
                new { trackName = (string?)null, artistName = "Nobody" },
                new { trackName = "Imagine", artistName = "John Lennon" },
            },
        });

        var track = await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct);

        track.ShouldBe(new Track("Imagine", "John Lennon"));
    }

    [Fact]
    public async Task Escapes_title_in_query()
    {
        GivenSearch("AC/DC & Co", new { results = new[] { new { trackName = "T.N.T.", artistName = "AC/DC" } } });

        var track = await CreateSut().FindAsync(new TrackQuery("AC/DC & Co", Weather.Sun), Ct);

        track.ShouldNotBeNull();
    }

    [Fact]
    public async Task Returns_null_when_nothing_matches()
    {
        GivenSearch("Unknown", new { resultCount = 0, results = Array.Empty<object>() });

        (await CreateSut().FindAsync(new TrackQuery("Unknown", Weather.Sun), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Returns_null_on_server_error()
    {
        _server.Given(Request.Create().WithPath("/search")).RespondWith(Response.Create().WithStatusCode(500));

        (await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Returns_null_on_malformed_json()
    {
        _server.Given(Request.Create().WithPath("/search")).RespondWith(Response.Create().WithStatusCode(200).WithBody("{ not json"));

        (await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Returns_null_on_timeout()
    {
        _server.Given(Request.Create().WithPath("/search"))
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)).WithBodyAsJson(new { results = Array.Empty<object>() }));

        (await CreateSut(TimeSpan.FromMilliseconds(200)).FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Sun), cancellation.Token));
    }

    [Fact]
    public async Task Rejects_a_query_without_title() =>
        await Should.ThrowAsync<ArgumentNullException>(() => CreateSut().FindAsync(new TrackQuery(null, Weather.Sun), Ct));
}
