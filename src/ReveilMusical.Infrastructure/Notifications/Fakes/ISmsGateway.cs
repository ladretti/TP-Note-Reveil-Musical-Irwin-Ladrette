namespace ReveilMusical.Infrastructure.Notifications.Fakes;

internal interface ISmsGateway
{
    bool Send(string phoneNumber, string text);
}
