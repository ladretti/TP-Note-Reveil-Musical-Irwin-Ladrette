using Microsoft.Extensions.Logging;

namespace ReveilMusical.Infrastructure.Notifications.Fakes;

internal sealed class FakeEmailClient(ILogger<FakeEmailClient> logger) : IEmailClient
{
    public Task SendMailAsync(string to, string subject, string htmlBody)
    {
        logger.LogInformation("[EMAIL] to {To} | {Subject} | {Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
