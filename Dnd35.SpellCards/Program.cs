using Dnd35.SpellCards.Infrastructure;
using Dnd35.SpellCards.Models;
using Dnd35.SpellCards.Rendering;
using Dnd35.SpellCards.Requests;
using Dnd35.SpellCards.Sources;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

FontManager.RegisterFont(
    File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/fonts/Cinzel-SemiBold.ttf")));

FontManager.RegisterFont(
    File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/fonts/SourceSerif4-Regular.ttf")));

FontManager.RegisterFont(
    File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/fonts/SourceSerif4-Semibold.ttf")));

QuestPDF.Settings.License = LicenseType.Community;

var ct = CancellationToken.None;

var baseDir = AppContext.BaseDirectory;
var reqPath = Path.Combine(baseDir, "requests.yaml");

Directory.CreateDirectory(Path.Combine(baseDir, "cache"));
Directory.CreateDirectory(Path.Combine(baseDir, "out"));

var requests = LoadRequests(reqPath);

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("Dnd35SpellCards/1.0 (personal use)");

var cache = new HttpCache(http, Path.Combine(baseDir, "cache"));
var source = new D20SrdSpellSource(cache);

var spells = await source.FetchSpellsAsync(requests, ct);

var finalSpells = spells
    .SelectMany(SpellSplitter.SplitIfNeeded)
    .ToList();

var output = Path.Combine(baseDir, "out", "spellcards.pdf");
new SpellCardDocument(finalSpells).GeneratePdf(output);

Console.WriteLine($"Generated: {output}");

static IReadOnlyList<SpellRequest> LoadRequests(string path)
{
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var yaml = File.ReadAllText(path);
    var root = deserializer.Deserialize<RequestFile>(yaml);
    return root?.Spells ?? [];
}
