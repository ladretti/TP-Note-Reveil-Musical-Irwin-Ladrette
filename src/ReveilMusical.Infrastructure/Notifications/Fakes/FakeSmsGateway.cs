using Microsoft.Extensions.Logging;

namespace ReveilMusical.Infrastructure.Notifications.Fakes;

internal sealed class FakeSmsGateway(ILogger<FakeSmsGateway> logger) : ISmsGateway
{
    public bool Send(string phoneNumber, string text)
    {
        logger.LogInformation("[SMS] to {PhoneNumber} | {Text}", phoneNumber, text);
        return true;
    }
}
