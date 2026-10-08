using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music.MusicBrainz;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ReveilMusical.Infrastructure.Tests.Music;

public sealed class MusicBrainzMusicProviderTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private MusicBrainzMusicProvider CreateSut() => new(
        new HttpClient { BaseAddress = new Uri(_server.Url!) },
        NullLogger<MusicBrainzMusicProvider>.Instance);

    private void GivenRecordings(string json) => _server
        .Given(Request.Create().WithPath("/ws/2/recording").WithParam("query", "Imagine").WithParam("fmt", "json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(json));

    [Fact]
    public async Task Maps_title_and_first_credited_artist()
    {
        GivenRecordings("""
            { "recordings": [ { "title": "Imagine", "artist-credit": [ { "name": "John Lennon" }, { "name": "Plastic Ono Band" } ] } ] }
            """);

        var track = await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Rain), Ct);

        track.ShouldBe(new Track("Imagine", "John Lennon"));
    }

    [Fact]
    public async Task Skips_recordings_without_artist()
    {
        GivenRecordings("""
            { "recordings": [ { "title": "Imagine", "artist-credit": [] }, { "title": "Imagine", "artist-credit": [ { "name": "A Perfect Circle" } ] } ] }
            """);

        var track = await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Rain), Ct);

        track.ShouldBe(new Track("Imagine", "A Perfect Circle"));
    }

    [Fact]
    public async Task Returns_null_when_nothing_matches()
    {
        GivenRecordings("""{ "recordings": [] }""");

        (await CreateSut().FindAsync(new TrackQuery("Imagine", Weather.Rain), Ct)).ShouldBeNull();
    }
}
