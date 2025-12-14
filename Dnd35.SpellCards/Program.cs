using Dnd35.SpellCards.Condensing;
using Dnd35.SpellCards.Infrastructure;
using Dnd35.SpellCards.Models;
using Dnd35.SpellCards.Rendering;
using Dnd35.SpellCards.Sources;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

var commandLine = CommandLineOptions.Parse(args);
var condenseDescriptions = !commandLine.NoCondense;

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

var settingsPath = Path.Combine(baseDir, "settings.json");
var settings = SpellCardsSettings.Load(settingsPath);

var cacheDir = Path.Combine(baseDir, "cache");
Directory.CreateDirectory(cacheDir);
Directory.CreateDirectory(Path.Combine(baseDir, "out"));

var requests = LoadRequests(reqPath);

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("Dnd35SpellCards/1.0 (personal use)");

var cache = new HttpCache(http, cacheDir);
var source = new D20SrdSpellSource(cache);

var spells = await source.FetchSpellsAsync(requests, ct);

ISpellCondenser condenser = new NoOpSpellCondenser();
OllamaSpellCondenser? ollama = null;

if (condenseDescriptions)
{
    var model = commandLine.Model
                ?? settings?.Ollama?.Model
                ?? Environment.GetEnvironmentVariable("SPELLCARDS_OLLAMA_MODEL")
                ?? "mixtral:8x7b";
    var endpoint = commandLine.Endpoint
                   ?? settings?.Ollama?.Endpoint
                   ?? Environment.GetEnvironmentVariable("SPELLCARDS_OLLAMA_ENDPOINT")
                   ?? "http://127.0.0.1:11434";

    if (!await OllamaSpellCondenser.IsServiceAvailableAsync(endpoint, ct))
    {
        Console.WriteLine($"[condense] Ollama endpoint '{endpoint}' is unreachable. Run ollama serve or pass --no-condense.");
    }
    else
    {
        try
        {
            var condenseCache = new SpellCondensingCache(Path.Combine(cacheDir, "condensed"));
            ollama = new OllamaSpellCondenser(model, endpoint, condenseCache);
            condenser = ollama;
            Console.WriteLine($"[condense] Using Ollama model '{model}' at {endpoint}. Pass --no-condense to disable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[condense] Disabled (falling back to raw text): {ex.Message}");
        }
    }
}

var finalSpells = await PrepareSpellCardsAsync(spells, condenser, ct);

ollama?.Dispose();

var output = Path.Combine(baseDir, "out", "spellcards.pdf");
new SpellCardDocument(finalSpells).GeneratePdf(output);

Console.WriteLine($"Generated: {output}");

static async Task<IReadOnlyList<Spell>> PrepareSpellCardsAsync(IReadOnlyList<Spell> spells, ISpellCondenser condenser, CancellationToken ct)
{
    var result = new List<Spell>();
    var canCondense = condenser is not NoOpSpellCondenser;

    foreach (var spell in spells)
    {
        var baselineParts = SpellSplitter.SplitIfNeeded(spell);
        if (!canCondense || baselineParts.Count == 1)
        {
            result.AddRange(baselineParts);
            continue;
        }

        try
        {
            var condensedText = await condenser.CondenseAsync(spell, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(condensedText))
            {
                result.AddRange(baselineParts);
                continue;
            }

            var condensedParts = SpellSplitter.SplitIfNeeded(spell with { Description = condensedText });
            if (condensedParts.Count < baselineParts.Count)
            {
                Console.WriteLine($"[condense] {spell.Name}: {baselineParts.Count} -> {condensedParts.Count} cards");
                result.AddRange(condensedParts);
            }
            else
            {
                result.AddRange(baselineParts);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[condense] Failed for {spell.Name}: {ex.Message}. Using original text.");
            result.AddRange(baselineParts);
        }
    }

    return result;
}

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

internal sealed record CommandLineOptions(bool NoCondense, string? Model, string? Endpoint)
{
    public static CommandLineOptions Parse(string[] args)
    {
        var noCondense = false;
        string? model = null;
        string? endpoint = null;

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--no-condense", StringComparison.OrdinalIgnoreCase))
            {
                noCondense = true;
                continue;
            }

            if (arg.StartsWith("--model=", StringComparison.OrdinalIgnoreCase))
            {
                model = arg[("--model=").Length..].Trim();
                continue;
            }

            if (arg.StartsWith("--endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = arg[("--endpoint=").Length..].Trim();
            }
        }

        return new CommandLineOptions(noCondense, string.IsNullOrWhiteSpace(model) ? null : model,
            string.IsNullOrWhiteSpace(endpoint) ? null : endpoint);
    }
}
