using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;
using ReveilMusical.Infrastructure.Users;

namespace ReveilMusical.Infrastructure.Tests.Users;

public sealed class LastKnownUserProfileProviderTests : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly UserProfile Profile = new("42", new Dictionary<SongSlot, string>(), "Wake Me Up", ChannelType.Push, new UserContact(PushToken: "device-42"));

    private readonly IUserProfileProvider _inner = Substitute.For<IUserProfileProvider>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly LastKnownUserProfileProvider _sut;

    public LastKnownUserProfileProviderTests() =>
        _sut = new LastKnownUserProfileProvider(_inner, _cache, NullLogger<LastKnownUserProfileProvider>.Instance);

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task Returns_and_caches_the_profile_on_success()
    {
        _inner.GetAsync("42", Ct).Returns(Profile);

        (await _sut.GetAsync("42", Ct)).ShouldBe(Profile);

        _cache.TryGetValue("profile:42", out UserProfile? cached).ShouldBeTrue();
        cached.ShouldBe(Profile);
    }

    [Fact]
    public async Task Serves_the_last_known_profile_when_the_inner_provider_throws()
    {
        _inner.GetAsync("42", Ct).Returns(Profile);
        await _sut.GetAsync("42", Ct);
        _inner.GetAsync("42", Ct).ThrowsAsync(new HttpRequestException("down"));

        (await _sut.GetAsync("42", Ct)).ShouldBe(Profile with { IsStale = true });
    }

    [Fact]
    public async Task Serves_the_last_known_profile_when_the_inner_provider_times_out()
    {
        _inner.GetAsync("42", Ct).Returns(Profile);
        await _sut.GetAsync("42", Ct);
        _inner.GetAsync("42", Ct).ThrowsAsync(new TaskCanceledException("timeout"));

        (await _sut.GetAsync("42", Ct)).ShouldBe(Profile with { IsStale = true });
    }

    [Fact]
    public async Task Rethrows_when_nothing_is_cached()
    {
        _inner.GetAsync("42", Ct).ThrowsAsync(new HttpRequestException("down"));

        await Should.ThrowAsync<HttpRequestException>(() => _sut.GetAsync("42", Ct));
    }

    [Fact]
    public async Task Evicts_and_returns_null_for_an_unknown_user()
    {
        _inner.GetAsync("42", Ct).Returns(Profile);
        await _sut.GetAsync("42", Ct);
        _inner.GetAsync("42", Ct).Returns((UserProfile?)null);

        (await _sut.GetAsync("42", Ct)).ShouldBeNull();

        _cache.TryGetValue("profile:42", out UserProfile? _).ShouldBeFalse();
    }

    [Fact]
    public async Task Propagates_caller_cancellation_even_with_a_cached_profile()
    {
        _inner.GetAsync("42", Ct).Returns(Profile);
        await _sut.GetAsync("42", Ct);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _inner.GetAsync("42", cts.Token).ThrowsAsync(new OperationCanceledException(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => _sut.GetAsync("42", cts.Token));
    }
}
