using System.Diagnostics;
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

Console.WriteLine();
Console.WriteLine("## Licences des paquets transitifs");

string[] propertyNames = ["topLevelPackages", "transitivePackages"];
string[] permissive = ["MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "MS-PL", "ISC", "0BSD", "Unlicense"];

using var listing = Process.Start(new ProcessStartInfo("dotnet", "list package --include-transitive --format json")
{
    RedirectStandardOutput = true,
})!;
var listingJson = await listing.StandardOutput.ReadToEndAsync();
await listing.WaitForExitAsync();

using var resolved = JsonDocument.Parse(listingJson);
var all = resolved.RootElement.GetProperty("projects").EnumerateArray()
    .Where(project => project.TryGetProperty("frameworks", out _))
    .SelectMany(project => project.GetProperty("frameworks").EnumerateArray())
    .SelectMany(framework => propertyNames
        .Where(name => framework.TryGetProperty(name, out _))
        .SelectMany(name => framework.GetProperty(name).EnumerateArray()))
    .Select(package => (Id: package.GetProperty("id").GetString()!, Version: package.GetProperty("resolvedVersion").GetString()!))
    .Distinct()
    .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
    .ToList();

var licenses = new List<(string Id, string Version, string License)>();
foreach (var (id, version) in all)
{
#pragma warning disable CA1308 // NuGet's v3 API only accepts lower-case package ids.
    var key = id.ToLowerInvariant();
#pragma warning restore CA1308
    var license = "inconnue";
    try
    {
        var nuspec = XDocument.Parse(await http.GetStringAsync(new Uri($"https://api.nuget.org/v3-flatcontainer/{key}/{version.ToLowerInvariant()}/{key}.nuspec")));
        var element = nuspec.Descendants().FirstOrDefault(e => e.Name.LocalName == "license");
        license = element is null ? "licenseUrl seule" : element.Value;
    }
    catch (HttpRequestException)
    {
    }

    licenses.Add((id, version, license));
}

static bool IsPermissive(string license, string[] allowed) =>
    license.Split([' ', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
        .All(token => token is "OR" or "AND" || allowed.Contains(token, StringComparer.Ordinal));

Console.WriteLine($"{licenses.Count} paquets distincts (directs et transitifs, tous projets).");
foreach (var group in licenses.GroupBy(entry => entry.License).OrderByDescending(group => group.Count()))
{
    Console.WriteLine($"- {group.Key} : {group.Count()}");
}

var flagged = licenses.Where(entry => !IsPermissive(entry.License, permissive)).ToList();
Console.WriteLine(flagged.Count == 0
    ? "Aucun paquet hors liste permissive (MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, MS-PL, ISC, 0BSD, Unlicense)."
    : "Hors liste permissive ou inconnus :");
foreach (var (id, version, license) in flagged)
{
    Console.WriteLine($"- {id} {version} : {license}");
}
