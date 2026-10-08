using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReveilMusical.Api.Tests;

public sealed class DegradedModeEndToEndTests
{
    [Fact]
    public async Task Wakes_the_user_with_the_local_catalog_when_every_music_provider_is_unreachable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Music:ITunes:BaseUrl", "http://127.0.0.1:9/")
            .UseSetting("Music:MusicBrainz:BaseUrl", "http://127.0.0.1:9/"));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            new Uri("/wakeups", UriKind.Relative),
            new { userId = "42", day = "Monday", weather = "PLUIE" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        body.GetProperty("track").GetProperty("title").GetString().ShouldBe("Singin' in the Rain");
        body.GetProperty("deliveredVia").GetString().ShouldBe("Push");
        body.GetProperty("reasons")[0].GetString().ShouldBe("MusicFallbackUsed");
    }
}
