using Dnd35.SpellCards.Infrastructure;
using Dnd35.SpellCards.Models;
using Dnd35.SpellCards.Rendering;
using Dnd35.SpellCards.Sources;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

FontManager.RegisterFont(
    File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/fonts/Cinzel-SemiBold.ttf")));

FontManager.RegisterFont(
    File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/fonts/SourceSerif4-Regular.ttf")));

FontManager.RegisterFont(
    File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets/fonts/SourceSerif4-Semibold.ttf")));

QuestPDF.Settings.License = LicenseType.Community;

var ct = CancellationToken.None;

var baseDir = AppContext.BaseDirectory;
var reqPath = Path.Combine(baseDir, "requests.txt");

if (!File.Exists(reqPath))
    throw new FileNotFoundException("requests.txt not found. Ensure it is copied next to the executable.", reqPath);

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
    var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var list = new List<SpellRequest>();

    foreach (var rawLine in File.ReadLines(path))
    {
        var line = rawLine.Split('#', 2)[0].Trim();
        if (string.IsNullOrEmpty(line))
            continue;

        if (unique.Add(line))
            list.Add(new SpellRequest { Name = line });
    }

    if (list.Count == 0)
        throw new InvalidOperationException($"No spell names found in '{path}'. Add at least one name (one per line).");

    return list;
}
