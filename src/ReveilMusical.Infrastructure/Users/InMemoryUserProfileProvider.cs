using System.Collections.Frozen;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Notifications;

namespace ReveilMusical.Infrastructure.Users;

internal sealed class InMemoryUserProfileProvider : IUserProfileProvider
{
    private readonly FrozenDictionary<string, UserProfile> _profiles = new UserProfile[]
    {
        new(
            "42",
            new Dictionary<SongSlot, string>
            {
                [new SongSlot(DayOfWeek.Monday, Weather.Rain)] = "Riders on the Storm",
                [new SongSlot(DayOfWeek.Monday, Weather.Sun)] = "Walking on Sunshine",
                [new SongSlot(DayOfWeek.Friday, Weather.Cloudy)] = "Mr. Blue Sky",
                [new SongSlot(DayOfWeek.Saturday, Weather.Snow)] = "Let It Go",
            },
            "Wake Me Up",
            Channels.Push,
            ContactOf((Channels.Email, "alice@example.com"), (Channels.Sms, "+33600000042"), (Channels.Push, "device-42"))),
        new("7", new Dictionary<SongSlot, string>(), "Good Morning", Channels.Sms, ContactOf((Channels.Sms, "+33600000007"))),
        new("13", new Dictionary<SongSlot, string>(), "Here Comes the Sun", Channels.Email, ContactOf((Channels.Push, "device-13"))),
    }.ToFrozenDictionary(profile => profile.UserId);

    private static UserContact ContactOf(params (ChannelType Channel, string Address)[] addresses) =>
        new(addresses.ToDictionary(entry => entry.Channel, entry => entry.Address));

    public Task<UserProfile?> GetAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(userId));
}
