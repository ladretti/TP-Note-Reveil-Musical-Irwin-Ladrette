using System.Collections.Frozen;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;

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
            ChannelType.Push,
            new UserContact("alice@example.com", "+33600000042", "device-42")),
        new("7", new Dictionary<SongSlot, string>(), "Good Morning", ChannelType.Sms, new UserContact(Phone: "+33600000007")),
        new("13", new Dictionary<SongSlot, string>(), "Here Comes the Sun", ChannelType.Email, new UserContact(PushToken: "device-13")),
    }.ToFrozenDictionary(profile => profile.UserId);

    public Task<UserProfile?> GetAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(userId));
}
