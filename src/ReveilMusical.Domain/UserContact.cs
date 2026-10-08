namespace ReveilMusical.Domain;

public sealed record UserContact(string? Email = null, string? Phone = null, string? PushToken = null)
{
    public static UserContact None { get; } = new();
}
