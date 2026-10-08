using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ReveilMusical.Domain.Ports;

namespace ReveilMusical.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public IUserProfileProvider Profiles { get; } = Substitute.For<IUserProfileProvider>();

    public ITrackResolver Tracks { get; } = Substitute.For<ITrackResolver>();

    public IWakeUpNotifier Notifier { get; } = Substitute.For<IWakeUpNotifier>();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
        .UseEnvironment("Production")
        .ConfigureTestServices(services => services
            .AddSingleton(Profiles)
            .AddSingleton(Tracks)
            .AddSingleton(Notifier));
}
