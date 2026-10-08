using Microsoft.Extensions.Logging;

namespace ReveilMusical.Infrastructure.Notifications.Fakes;

internal sealed class FakePushService(ILogger<FakePushService> logger) : IPushService
{
    public Task<PushReceipt> PushAsync(PushPayload payload)
    {
        logger.LogInformation("[PUSH] to {DeviceToken} | {Title} | {Body}", payload.DeviceToken, payload.Title, payload.Body);
        return Task.FromResult(new PushReceipt(Guid.NewGuid().ToString("N"), Accepted: true));
    }
}
