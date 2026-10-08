namespace ReveilMusical.Infrastructure.Notifications.Fakes;

internal interface IPushService
{
    Task<PushReceipt> PushAsync(PushPayload payload);
}

internal sealed record PushPayload(string DeviceToken, string Title, string Body);

internal sealed record PushReceipt(string MessageId, bool Accepted);
