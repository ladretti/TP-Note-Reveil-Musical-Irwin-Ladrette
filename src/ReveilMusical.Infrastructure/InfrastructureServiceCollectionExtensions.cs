using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Music.ITunes;
using ReveilMusical.Infrastructure.Music.MusicBrainz;
using ReveilMusical.Infrastructure.Notifications;
using ReveilMusical.Infrastructure.Notifications.Fakes;
using ReveilMusical.Infrastructure.Options;
using ReveilMusical.Infrastructure.Users;

namespace ReveilMusical.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var musicSection = configuration.GetSection(MusicOptions.SectionName);
        services.AddOptions<MusicOptions>()
            .Bind(musicSection)
            .Configure(_ => RejectUnknownProviderNames(musicSection))
            .ValidateOnStart();
        services.AddOptions<NotificationOptions>().Bind(configuration.GetSection(NotificationOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<MusicOptions>, MusicOptionsValidator>();
        services.AddSingleton<IValidateOptions<NotificationOptions>, NotificationOptionsValidator>();

        services.AddSingleton<IUserProfileProvider, InMemoryUserProfileProvider>();
        services.AddMusic();
        services.AddNotifications();
        return services;
    }

    // The binder silently drops list items it cannot convert, which would hide a typo in Music:Providers.
    private static void RejectUnknownProviderNames(IConfigurationSection musicSection)
    {
        foreach (var name in musicSection.GetSection(nameof(MusicOptions.Providers)).GetChildren().Select(child => child.Value))
        {
            if (!Enum.TryParse<MusicProviderKind>(name, out _))
            {
                throw new InvalidOperationException($"Unknown music provider '{name}' in configuration.");
            }
        }
    }

    private static void AddMusic(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddRemoteMusicProvider<ITunesMusicProvider>(MusicProviderKind.ITunes, options => options.ITunes);
        services.AddRemoteMusicProvider<MusicBrainzMusicProvider>(MusicProviderKind.MusicBrainz, options => options.MusicBrainz);
        services.AddSingleton<LocalMusicProvider>();
        services.AddTransient<ITrackResolver>(provider => ActivatorUtilities.CreateInstance<FallbackTrackResolver>(
            provider,
            provider.GetRequiredService<IOptions<MusicOptions>>().Value.Providers
                .Select(kind => provider.GetRequiredKeyedService<IMusicProvider>(kind))
                .ToList()));
    }

    private static void AddRemoteMusicProvider<TProvider>(
        this IServiceCollection services,
        MusicProviderKind kind,
        Func<MusicOptions, RemoteProviderOptions> select)
        where TProvider : class, IMusicProvider
    {
        RemoteProviderOptions OptionsOf(IServiceProvider provider) => select(provider.GetRequiredService<IOptions<MusicOptions>>().Value);

        services.AddHttpClient<TProvider>((provider, client) =>
        {
            var options = OptionsOf(provider);
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        // One limiter per provider for the whole process: a per-request limiter would not protect the quota.
        services.AddKeyedSingleton<RateLimiter>(kind, (provider, _) =>
        {
            var options = OptionsOf(provider);
            return ActivatorUtilities.CreateInstance<FixedWindowRateLimiter>(provider, new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.PermitsPerWindow,
                Window = options.Window,
                QueueLimit = 0,
            });
        });

        services.AddKeyedTransient<IMusicProvider>(kind, (provider, _) =>
        {
            var rateLimited = ActivatorUtilities.CreateInstance<RateLimitedMusicProvider>(
                provider,
                provider.GetRequiredService<TProvider>(),
                provider.GetRequiredKeyedService<RateLimiter>(kind));
            return ActivatorUtilities.CreateInstance<CachingMusicProvider>(provider, rateLimited);
        });
    }

    private static void AddNotifications(this IServiceCollection services)
    {
        services.AddSingleton<IEmailClient, FakeEmailClient>();
        services.AddSingleton<ISmsGateway, FakeSmsGateway>();
        services.AddSingleton<IPushService, FakePushService>();
        services.AddTransient<INotificationChannel, EmailChannel>();
        services.AddTransient<INotificationChannel, SmsChannel>();
        services.AddTransient<INotificationChannel, PushChannel>();
        services.AddTransient<IWakeUpNotifier, FallbackNotifier>();
    }
}
