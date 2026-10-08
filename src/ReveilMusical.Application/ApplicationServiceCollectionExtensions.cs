using Microsoft.Extensions.DependencyInjection;

namespace ReveilMusical.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddScoped<WakeUpService>();
}
