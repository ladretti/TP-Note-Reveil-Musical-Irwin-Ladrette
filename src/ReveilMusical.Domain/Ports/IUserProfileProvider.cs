namespace ReveilMusical.Domain.Ports;

public interface IUserProfileProvider
{
    /// <returns><c>null</c> when the user does not exist; throws when the service is unavailable.</returns>
    Task<UserProfile?> GetAsync(string userId, CancellationToken cancellationToken);
}
