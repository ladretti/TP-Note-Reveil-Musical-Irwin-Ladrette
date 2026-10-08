using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;

using var http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

var packages = XDocument.Load("Directory.Packages.props").Descendants("PackageVersion")
    .Select(element => (Id: element.Attribute("Include")!.Value, Version: element.Attribute("Version")!.Value))
    .Concat(JsonDocument.Parse(File.ReadAllText(".config/dotnet-tools.json")).RootElement.GetProperty("tools").EnumerateObject()
        .Select(tool => (Id: tool.Name, Version: tool.Value.GetProperty("version").GetString()!)))
    .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase);

Console.WriteLine("| Paquet | Installée | Dernière stable | Publiée le | Licence | Statut |");
Console.WriteLine("|---|---|---|---|---|---|");

foreach (var (id, installed) in packages)
{
#pragma warning disable CA1308 // NuGet's v3 API only accepts lower-case package ids.
    var key = id.ToLowerInvariant();
#pragma warning restore CA1308

    using var index = JsonDocument.Parse(await http.GetStringAsync(new Uri($"https://api.nuget.org/v3-flatcontainer/{key}/index.json")));
    var latest = index.RootElement.GetProperty("versions").EnumerateArray()
        .Select(version => version.GetString()!)
        .Last(version => !version.Contains('-', StringComparison.Ordinal));

    using var leaf = JsonDocument.Parse(await http.GetStringAsync(new Uri($"https://api.nuget.org/v3/registration5-gz-semver2/{key}/{latest}.json")));
    var published = leaf.RootElement.GetProperty("published").GetDateTimeOffset().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    var nuspec = XDocument.Parse(await http.GetStringAsync(new Uri($"https://api.nuget.org/v3-flatcontainer/{key}/{installed}/{key}.nuspec")));
    var license = nuspec.Descendants().FirstOrDefault(element => element.Name.LocalName == "license")?.Value ?? "voir licenseUrl";

    Console.WriteLine($"| {id} | {installed} | {latest} | {published} | {license} | {(installed == latest ? "à jour" : "en retard")} |");
}
