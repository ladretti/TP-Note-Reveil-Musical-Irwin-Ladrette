using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Notifications;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Tests.Notifications;

public sealed class RegisteredChannelsValidatorTests
{
    private static INotificationChannel ChannelOf(string name)
    {
        var channel = Substitute.For<INotificationChannel>();
        channel.Type.Returns(new ChannelType(name));
        return channel;
    }

    private static readonly NotificationOptions Options = new() { FallbackOrder = ["Sms"] };

    [Fact]
    public void Accepts_a_fallback_order_made_of_registered_channels() =>
        new RegisteredChannelsValidator([ChannelOf("Sms"), ChannelOf("Push")]).Validate(null, Options).Succeeded.ShouldBeTrue();

    [Fact]
    public void Rejects_two_channels_declaring_the_same_name()
    {
        var result = new RegisteredChannelsValidator([ChannelOf("Sms"), ChannelOf("sms")]).Validate(null, Options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Sms");
    }
}
