using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ReveilMusical.Domain;
using ReveilMusical.Infrastructure.Music;
using ReveilMusical.Infrastructure.Options;

namespace ReveilMusical.Infrastructure.Tests.Music;

public sealed class CachingMusicProviderTests : IDisposable
{
    private static readonly Track Imagine = new("Imagine", "John Lennon");
    private readonly IMusicProvider _inner = Substitute.For<IMusicProvider>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly CachingMusicProvider _sut;

    public CachingMusicProviderTests()
    {
        _inner.Name.Returns("ITunes");
        _sut = new CachingMusicProvider(_inner, _cache, Microsoft.Extensions.Options.Options.Create(new MusicOptions { CacheTtl = TimeSpan.FromMinutes(5) }));
    }

    public void Dispose() => _cache.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Serves_repeated_titles_from_cache_ignoring_case_and_spaces()
    {
        _inner.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns(Imagine);

        await _sut.FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct);
        var second = await _sut.FindAsync(new TrackQuery("  imagine ", Weather.Rain), Ct);

        second.ShouldBe(Imagine);
        await _inner.Received(1).FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_cache_misses()
    {
        _inner.FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>()).Returns((Track?)null);

        await _sut.FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct);
        await _sut.FindAsync(new TrackQuery("Imagine", Weather.Sun), Ct);

        await _inner.Received(2).FindAsync(Arg.Any<TrackQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Exposes_the_inner_provider_name() => _sut.Name.ShouldBe("ITunes");
}
