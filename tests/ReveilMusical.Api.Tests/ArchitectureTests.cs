using ReveilMusical.Application;
using ReveilMusical.Domain;

namespace ReveilMusical.Api.Tests;

public sealed class ArchitectureTests
{
    private static readonly string[] TechnicalAssemblies =
    [
        "ReveilMusical.Infrastructure",
        "System.Net.Http",
        "System.Net.Http.Json",
        "System.Text.Json",
        "Microsoft.Extensions.Http",
        "Microsoft.Extensions.Caching.Memory",
    ];

    [Theory]
    [InlineData(typeof(Track))]
    [InlineData(typeof(WakeUpService))]
    public void Business_layers_know_no_technical_detail(Type marker) =>
        marker.Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Intersect(TechnicalAssemblies)
            .ShouldBeEmpty();
}
