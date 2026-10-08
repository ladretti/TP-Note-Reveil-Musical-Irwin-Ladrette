using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Options;
using ReveilMusical.Infrastructure.Users;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ReveilMusical.Infrastructure.Tests;

public sealed class DependencyInjectionTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Dictionary<string, string?> ValidSettings() => new()
    {
        ["Music:Providers:0"] = "MusicBrainz",
        ["Music:Providers:1"] = "ITunes",
        ["Music:CacheTtl"] = "00:10:00",
        ["Music:ITunes:BaseUrl"] = _server.Url,
        ["Music:ITunes:Timeout"] = "00:00:02",
        ["Music:ITunes:PermitsPerWindow"] = "20",
        ["Music:ITunes:Window"] = "00:01:00",
        ["Music:ITunes:UserAgent"] = "ReveilMusical-Tests/1.0 ( tests@example.com )",
        ["Music:MusicBrainz:BaseUrl"] = _server.Url,
        ["Music:MusicBrainz:Timeout"] = "00:00:02",
        ["Music:MusicBrainz:PermitsPerWindow"] = "1",
        ["Music:MusicBrainz:Window"] = "01:00:00",
        ["Music:MusicBrainz:UserAgent"] = "ReveilMusical-Tests/1.0 ( tests@example.com )",
        ["Notifications:FallbackOrder:0"] = "Push",
        ["Notifications:FallbackOrder:1"] = "Sms",
        ["Notifications:FallbackOrder:2"] = "Email",
    };

    private static ServiceProvider Build(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddLogging()
            .AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private void GivenMusicBrainz(string title, string artist) => _server
        .Given(Request.Create().WithPath("/ws/2/recording").WithParam("query", title))
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
            .WithBody($$"""{ "recordings": [ { "title": "{{title}}", "artist-credit": [ { "name": "{{artist}}" } ] } ] }"""));

    private void GivenITunes(string title, string artist) => _server
        .Given(Request.Create().WithPath("/search").WithParam("term", title))
        .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { results = new[] { new { trackName = title, artistName = artist } } }));

    [Fact]
    public async Task Resolves_the_user_profile_provider_as_the_last_known_decorator_over_the_in_memory_provider()
    {
        using var provider = Build(ValidSettings());

        var profiles = provider.GetRequiredService<IUserProfileProvider>();

        profiles.ShouldBeOfType<LastKnownUserProfileProvider>();
        (await profiles.GetAsync("42", Ct)).ShouldNotBeNull().UserId.ShouldBe("42");
    }

    [Fact]
    public async Task Resolves_with_the_configured_provider_order_and_an_identifiable_user_agent()
    {
        GivenMusicBrainz("Imagine", "John Lennon");
        using var provider = Build(ValidSettings());

        var resolved = await provider.GetRequiredService<ITrackResolver>().ResolveAsync(new TrackQuery("Imagine", Weather.Sun), Ct);

        resolved.ShouldBe(new ResolvedTrack(new Track("Imagine", "John Lennon"), IsFallback: false));
        _server.LogEntries.Single().RequestMessage!.Headers!["User-Agent"].ShouldContain("ReveilMusical-Tests/1.0 ( tests@example.com )");
    }

    [Fact]
    public async Task Caches_in_front_of_the_quota_so_repeated_titles_cost_nothing()
    {
        GivenMusicBrainz("Imagine", "John Lennon");
        using var provider = Build(ValidSettings());
        var resolver = provider.GetRequiredService<ITrackResolver>();

        await resolver.ResolveAsync(new TrackQuery("Imagine", Weather.Sun), Ct);
        var second = await resolver.ResolveAsync(new TrackQuery("Imagine", Weather.Sun), Ct);

        second.IsFallback.ShouldBeFalse();
        _server.LogEntries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Falls_back_to_the_next_provider_once_the_quota_is_spent()
    {
        GivenMusicBrainz("Imagine", "John Lennon");
        GivenITunes("Yesterday", "The Beatles");
        using var provider = Build(ValidSettings());
        var resolver = provider.GetRequiredService<ITrackResolver>();

        await resolver.ResolveAsync(new TrackQuery("Imagine", Weather.Sun), Ct);
        var second = await resolver.ResolveAsync(new TrackQuery("Yesterday", Weather.Sun), Ct);

        second.ShouldBe(new ResolvedTrack(new Track("Yesterday", "The Beatles"), IsFallback: true));
    }

    [Fact]
    public async Task Wires_notifications_and_user_profiles()
    {
        using var provider = Build(ValidSettings());
        var profile = await provider.GetRequiredService<IUserProfileProvider>().GetAsync("42", Ct);
        var message = WakeUpMessage.For("42", profile!.Contact, DayOfWeek.Monday, Weather.Rain, new Track("Imagine", "John Lennon"));

        var result = await provider.GetRequiredService<IWakeUpNotifier>().NotifyAsync(message, profile.PreferredChannel, Ct);

        result.ShouldBe(new NotificationResult(ChannelType.Push, UsedFallback: false));
    }

    [Fact]
    public void Missing_user_agent_fails_validation()
    {
        var settings = ValidSettings();
        settings.Remove("Music:MusicBrainz:UserAgent");
        using var provider = Build(settings);

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MusicOptions>>().Value);
    }

    [Fact]
    public void Empty_fallback_order_fails_validation()
    {
        var settings = ValidSettings();
        foreach (var key in settings.Keys.Where(k => k.StartsWith("Notifications:", StringComparison.Ordinal)).ToList())
        {
            settings.Remove(key);
        }

        using var provider = Build(settings);

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<NotificationOptions>>().Value);
    }

    [Fact]
    public void Unknown_provider_name_fails_options_binding()
    {
        var settings = ValidSettings();
        settings["Music:Providers:0"] = "Spotify";
        using var provider = Build(settings);

        Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IOptions<MusicOptions>>().Value);
    }

    [Fact]
    public void Unknown_channel_name_fails_options_binding()
    {
        var settings = ValidSettings();
        settings["Notifications:FallbackOrder:1"] = "Emial";
        using var provider = Build(settings);

        Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IOptions<NotificationOptions>>().Value);
    }
}
