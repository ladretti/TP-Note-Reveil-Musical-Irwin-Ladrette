namespace ReveilMusical.Domain.Tests;

public sealed class ChannelTypeTests
{
    [Fact]
    public void Channels_compare_by_name_ignoring_case_and_spaces()
    {
        new ChannelType(" sms ").ShouldBe(new ChannelType("Sms"));
        new ChannelType("sms").GetHashCode().ShouldBe(new ChannelType("SMS").GetHashCode());
        new ChannelType("Sms").ToString().ShouldBe("Sms");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_channel_needs_a_name(string name) => Should.Throw<ArgumentException>(() => new ChannelType(name));

    [Fact]
    public void Contact_gives_the_address_of_a_channel_or_null()
    {
        var contact = new UserContact(new Dictionary<ChannelType, string> { [new ChannelType("WhatsApp")] = "+33600000000" });

        contact.AddressFor(new ChannelType("whatsapp")).ShouldBe("+33600000000");
        contact.AddressFor(new ChannelType("Sms")).ShouldBeNull();
        UserContact.None.AddressFor(new ChannelType("Sms")).ShouldBeNull();
    }
}
