using Microsoft.Extensions.Logging.Abstractions;
using ReveilMusical.Infrastructure.Notifications.Fakes;

namespace ReveilMusical.Infrastructure.Tests.Notifications;

public sealed class FakeClientsTests
{
    [Fact]
    public async Task Fakes_simulate_successful_deliveries()
    {
        await new FakeEmailClient(NullLogger<FakeEmailClient>.Instance).SendMailAsync("a@example.com", "s", "<p>b</p>");
        new FakeSmsGateway(NullLogger<FakeSmsGateway>.Instance).Send("+33600000000", "b").ShouldBeTrue();
        var receipt = await new FakePushService(NullLogger<FakePushService>.Instance).PushAsync(new PushPayload("token", "t", "b"));

        receipt.Accepted.ShouldBeTrue();
        receipt.MessageId.ShouldNotBeNullOrWhiteSpace();
    }
}
