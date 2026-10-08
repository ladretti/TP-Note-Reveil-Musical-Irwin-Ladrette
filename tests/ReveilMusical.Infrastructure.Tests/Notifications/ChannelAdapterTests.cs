using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;
using ReveilMusical.Infrastructure.Notifications.Fakes;

namespace ReveilMusical.Infrastructure.Tests.Notifications;

public sealed class ChannelAdapterTests
{
    private static readonly UserContact FullContact = new("alice@example.com", "+33600000001", "device-alice");

    private static WakeUpMessage MessageFor(UserContact contact) => new("42", contact, "Réveil musical", "Bon lundi <pluvieux> !");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Email_sends_html_encoded_body_to_the_user_address()
    {
        var client = Substitute.For<IEmailClient>();

        var sent = await new EmailChannel(client).TrySendAsync(MessageFor(FullContact), Ct);

        sent.ShouldBeTrue();
        await client.Received(1).SendMailAsync("alice@example.com", "Réveil musical", "<p>Bon lundi &lt;pluvieux&gt; !</p>");
    }

    [Fact]
    public async Task Sms_sends_plain_body_to_the_user_phone()
    {
        var gateway = Substitute.For<ISmsGateway>();
        gateway.Send(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        (await new SmsChannel(gateway).TrySendAsync(MessageFor(FullContact), Ct)).ShouldBeTrue();
        gateway.Received(1).Send("+33600000001", "Bon lundi <pluvieux> !");
    }

    [Fact]
    public async Task Sms_reports_gateway_refusal()
    {
        var gateway = Substitute.For<ISmsGateway>();
        gateway.Send(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        (await new SmsChannel(gateway).TrySendAsync(MessageFor(FullContact), Ct)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Push_maps_message_to_payload_and_reports_receipt(bool accepted)
    {
        var service = Substitute.For<IPushService>();
        service.PushAsync(Arg.Any<PushPayload>()).Returns(new PushReceipt("id-1", accepted));

        (await new PushChannel(service).TrySendAsync(MessageFor(FullContact), Ct)).ShouldBe(accepted);
        await service.Received(1).PushAsync(new PushPayload("device-alice", "Réveil musical", "Bon lundi <pluvieux> !"));
    }

    [Fact]
    public async Task Channels_decline_when_the_contact_is_missing()
    {
        var email = Substitute.For<IEmailClient>();
        var sms = Substitute.For<ISmsGateway>();
        var push = Substitute.For<IPushService>();
        var message = MessageFor(UserContact.None);

        (await new EmailChannel(email).TrySendAsync(message, Ct)).ShouldBeFalse();
        (await new SmsChannel(sms).TrySendAsync(message, Ct)).ShouldBeFalse();
        (await new PushChannel(push).TrySendAsync(message, Ct)).ShouldBeFalse();
        email.ReceivedCalls().ShouldBeEmpty();
        sms.ReceivedCalls().ShouldBeEmpty();
        push.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void Channels_declare_their_type()
    {
        new EmailChannel(Substitute.For<IEmailClient>()).Type.ShouldBe(ChannelType.Email);
        new SmsChannel(Substitute.For<ISmsGateway>()).Type.ShouldBe(ChannelType.Sms);
        new PushChannel(Substitute.For<IPushService>()).Type.ShouldBe(ChannelType.Push);
    }
}
