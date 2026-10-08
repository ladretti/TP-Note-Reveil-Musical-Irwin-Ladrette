namespace ReveilMusical.Domain;

public sealed record Track(string Title, string Artist, Uri? ListenUrl = null);
