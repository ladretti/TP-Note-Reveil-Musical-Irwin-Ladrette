namespace ReveilMusical.Domain;

/// <summary>Opaque channel identifier ("Sms", "WhatsApp"...): the business layer knows none of them.</summary>
public readonly record struct ChannelType
{
    public ChannelType(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public string Name { get; }

    public bool Equals(ChannelType other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public override string ToString() => Name ?? string.Empty;
}
