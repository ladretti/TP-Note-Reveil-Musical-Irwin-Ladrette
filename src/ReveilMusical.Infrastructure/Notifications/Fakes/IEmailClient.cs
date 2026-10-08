namespace ReveilMusical.Infrastructure.Notifications.Fakes;

internal interface IEmailClient
{
    Task SendMailAsync(string to, string subject, string htmlBody);
}
