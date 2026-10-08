namespace ReveilMusical.Domain;

public sealed record UserContact(IReadOnlyDictionary<ChannelType, string> Addresses)
{
    public static UserContact None { get; } = new(new Dictionary<ChannelType, string>());

    public string? AddressFor(ChannelType channel) => Addresses.GetValueOrDefault(channel);
}
