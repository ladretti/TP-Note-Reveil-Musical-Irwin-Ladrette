using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;

namespace ReveilMusical.Infrastructure.Users;

internal sealed class LastKnownUserProfileProvider(
    IUserProfileProvider inner,
    IMemoryCache cache,
    ILogger<LastKnownUserProfileProvider> logger) : IUserProfileProvider
{
    public async Task<UserProfile?> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var key = $"profile:{userId}";
        try
        {
            var profile = await inner.GetAsync(userId, cancellationToken);
            if (profile is null)
            {
                cache.Remove(key);
            }
            else
            {
                cache.Set(key, profile);
            }

            return profile;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && cache.TryGetValue(key, out UserProfile? lastKnown) && lastKnown is not null)
        {
            logger.LogWarning(exception, "Profile service unavailable, using the last known profile of user {UserId}", userId);
            return lastKnown;
        }
    }
}
