using System.Text.Json;
using System.Text.Json.Serialization;
using SpellCards.Infrastructure;
using SpellCards.Models;

namespace SpellCards.Sources;

public sealed class Open5eSpellSource
{
    private const string ApiBase = "https://api.open5e.com";

    // NOTE: Open5e v2 exposes separate 2014 and 2024 SRD documents.
    // We follow "next" links when needed.
    private const string SearchEndpoint = ApiBase + "/v2/spells/?search=";

    private readonly HttpCache _cache;
    private readonly string _documentKey;
    private readonly string _rulesLabel;

    public Open5eSpellSource(HttpCache cache, string documentKey = "srd-2014", string rulesLabel = "5e")
    {
        _cache = cache;
        _documentKey = string.IsNullOrWhiteSpace(documentKey) ? "srd-2014" : documentKey.Trim();
        _rulesLabel = string.IsNullOrWhiteSpace(rulesLabel) ? "5e" : rulesLabel.Trim();
    }

    public async Task<IReadOnlyList<Spell>> FetchSpellsAsync(IReadOnlyList<string> spellNames, CancellationToken ct)
    {
        var result = new List<Spell>(spellNames.Count);

        foreach (var rawRequested in spellNames)
        {
            var requested = (rawRequested ?? string.Empty).Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(requested))
                continue;

            try
            {
                var (chosen, firstUrlUsed) = await FetchSingleAsync(requested, ct).ConfigureAwait(false);
                result.Add(MapToSpell(chosen, requested, firstUrlUsed));
            }
            catch (Exception ex)
            {
                result.Add(BuildNotFoundSpell(requested, ex.Message));
            }
        }

        return result;
    }

    private static Spell BuildNotFoundSpell(string requestedName, string reason)
    {
        var msg = string.IsNullOrWhiteSpace(reason)
            ? "Spell was not found. Please check spelling / spell name."
            : $"Spell was not found. Please check spelling / spell name.\n\nDetails: {reason}";

        return new Spell
        {
            Name = string.IsNullOrWhiteSpace(requestedName) ? "(unknown spell)" : requestedName,
            Part = string.Empty,
            ClassLevel = "",
            SchoolText = "",
            SchoolKey = "",
            Cast = "",
            Range = "",
            TargetOrArea = "",
            Duration = "",
            Save = "",
            Sr = "",
            Components = "",
            Tags = "Not Found",
            Description = msg,
            Notes = "",
            SourceUrl = ""
        };
    }

    private bool IsOfficial(Open5eDocumentInfo document)
    {
        return string.Equals(document.Key, _documentKey, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Open5eSpellData Chosen, string FirstUrlUsed)> FetchSingleAsync(string requested, CancellationToken ct)
    {
        // Try to reduce paging issues by requesting a larger page size.
        // We still follow "next" if present.
        var firstUrl = SearchEndpoint + Uri.EscapeDataString(requested)
                     + "&limit=200&document__key=" + Uri.EscapeDataString(_documentKey);

        var allOfficialCandidates = new List<Open5eSpellData>();

        string? url = firstUrl;
        var pages = 0;

        while (!string.IsNullOrWhiteSpace(url))
        {
            ct.ThrowIfCancellationRequested();

            pages++;
            if (pages > 10)
                break;

            var json = await _cache.GetStringCachedAsync(url, ct).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<Open5eV2SearchResponse>(json)
                           ?? throw new InvalidOperationException($"Open5e returned invalid JSON for '{requested}'.");

            var results = response.Results
                .Select(MapFromV2)
                .ToList();

            if (results.Count > 0)
                allOfficialCandidates.AddRange(results.Where(r => IsOfficial(r.Document)));

            // Fast-path: exact match found on this page (official only).
            var exactOfficialOnPage = results
                .Where(r => IsOfficial(r.Document))
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && string.Equals(r.Name, requested, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactOfficialOnPage.Count == 1)
                return (exactOfficialOnPage[0], firstUrl);

            if (exactOfficialOnPage.Count > 1)
            {
                var docs = string.Join(", ", exactOfficialOnPage.Select(x => x.Document.Title).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase));
                throw new InvalidOperationException($"Spell '{requested}' is ambiguous in Open5e official sources. Documents: {docs}");
            }

            url = response.Next;
        }

        if (allOfficialCandidates.Count == 0)
            throw new InvalidOperationException($"Spell '{requested}' not found in Open5e {_rulesLabel} sources ({_documentKey}).");

        if (allOfficialCandidates.Count == 1)
            return (allOfficialCandidates[0], firstUrl);

        var names = string.Join(", ", allOfficialCandidates.Select(x => x.Name).Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(12));
        throw new InvalidOperationException($"Spell '{requested}' not found exactly in Open5e {_rulesLabel} sources. Candidates: {names}");
    }

    private static Open5eSpellData MapFromV2(Open5eV2SpellDto spell)
    {
        return new Open5eSpellData
        {
            Name = spell.Name,
            Description = spell.Desc,
            HigherLevel = spell.HigherLevel,
            Range = spell.RangeText,
            Components = BuildComponents(spell.Verbal, spell.Somatic, spell.Material),
            Material = spell.MaterialSpecified,
            Ritual = spell.Ritual ? "yes" : "no",
            Concentration = spell.Concentration ? "yes" : "no",
            CastingTime = NormalizeCastingTime(spell.CastingTime),
            Duration = spell.Duration,
            School = spell.School?.Name,
            SchoolKey = spell.School?.Key,
            LevelInt = spell.Level,
            DndClass = string.Join(", ", spell.Classes.Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase)),
            AreaSize = spell.ShapeSize,
            AreaType = spell.ShapeType,
            AreaUnit = spell.ShapeSizeUnit,
            Document = new Open5eDocumentInfo(
                spell.Document?.Key,
                spell.Document?.DisplayName ?? spell.Document?.Name,
                spell.Document?.Permalink)
        };
    }

    private static Spell MapToSpell(Open5eSpellData s, string requestedName, string searchUrl)
    {
        var tags = new List<string>();
        var isRitual = string.Equals(s.Ritual, "yes", StringComparison.OrdinalIgnoreCase);
        var isConcentration = string.Equals(s.Concentration, "yes", StringComparison.OrdinalIgnoreCase);
        if (isRitual) tags.Add("Ritual");
        if (isConcentration) tags.Add("Concentration");

        var duration = s.Duration?.Trim() ?? string.Empty;
        if (isConcentration && !duration.Contains("concentration", StringComparison.OrdinalIgnoreCase))
            duration = string.IsNullOrWhiteSpace(duration) ? "Concentration" : "Concentration, " + duration;

        var components = NormalizeComponents(s.Components ?? string.Empty);

        var description = (s.Description ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(s.HigherLevel))
            description += (description.Length == 0 ? string.Empty : "\n\n") + "At Higher Levels. " + s.HigherLevel.Trim();

        var notes = string.IsNullOrWhiteSpace(s.Material) ? string.Empty : $"M: {s.Material.Trim()}";
        var classLevel = BuildClassLevel(s);

        var schoolText = string.IsNullOrWhiteSpace(s.School) ? string.Empty : s.School.Trim();
        var schoolKey = string.IsNullOrWhiteSpace(s.SchoolKey)
            ? ""
            : s.SchoolKey.Trim().ToLowerInvariant().Replace(" ", string.Empty);

        var targetOrArea = BuildTargetOrArea(s);

        return new Spell
        {
            Name = s.Name ?? requestedName,
            Part = string.Empty,
            ClassLevel = classLevel,
            SchoolText = schoolText,
            SchoolKey = schoolKey,
            Cast = s.CastingTime?.Trim() ?? string.Empty,
            Range = s.Range?.Trim() ?? string.Empty,
            TargetOrArea = targetOrArea,
            Duration = duration,
            Save = string.Empty,
            Sr = string.Empty,
            Components = components,
            Tags = string.Join(" \u00B7 ", tags),
            Description = description,
            Notes = notes,
            SourceUrl = s.Document.Url ?? searchUrl
        };
    }

    private static string NormalizeComponents(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        // Open5e commonly uses "V,S,M". Normalize to "V S M".
        var cleaned = raw.Replace(",", " ");
        return string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string BuildComponents(bool verbal, bool somatic, bool material)
    {
        var parts = new List<string>();
        if (verbal) parts.Add("V");
        if (somatic) parts.Add("S");
        if (material) parts.Add("M");
        return string.Join(" ", parts);
    }

    private static string NormalizeCastingTime(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        return value.ToLowerInvariant() switch
        {
            "action" => "1 action",
            "bonus action" => "1 bonus action",
            "reaction" => "1 reaction",
            _ => value
        };
    }

    private static string BuildClassLevel(Open5eSpellData s)
    {
        var classes = (s.DndClass ?? string.Empty).Trim();
        var firstClass = classes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (s.LevelInt == 0)
        {
            if (!string.IsNullOrWhiteSpace(firstClass))
                return $"{firstClass} Cantrip";
            return "Cantrip";
        }

        if (!string.IsNullOrWhiteSpace(firstClass))
            return $"{firstClass} {s.LevelInt}";

        return $"Level {s.LevelInt}";
    }

    private static string BuildTargetOrArea(Open5eSpellData s)
    {
        if (s.AreaSize is null || string.IsNullOrWhiteSpace(s.AreaType))
            return string.Empty;

        var unit = NormalizeAreaUnit(s.AreaUnit, s.AreaSize.Value);
        return string.IsNullOrWhiteSpace(unit)
            ? $"Area: {s.AreaSize} {s.AreaType.Trim()}"
            : $"Area: {s.AreaSize}-{unit} {s.AreaType.Trim()}";
    }

    private static string NormalizeAreaUnit(string? rawUnit, int size)
    {
        var unit = rawUnit?.Trim().ToLowerInvariant();
        return unit switch
        {
            "feet" when size == 1 => "foot",
            "feet" => "feet",
            _ => unit ?? string.Empty
        };
    }

    private sealed class Open5eV2SearchResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; init; }

        [JsonPropertyName("next")]
        public string? Next { get; init; }

        [JsonPropertyName("results")]
        public List<Open5eV2SpellDto> Results { get; init; } = new();
    }

    private sealed class Open5eV2SpellDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("desc")]
        public string? Desc { get; init; }

        [JsonPropertyName("higher_level")]
        public string? HigherLevel { get; init; }

        [JsonPropertyName("range_text")]
        public string? RangeText { get; init; }

        [JsonPropertyName("verbal")]
        public bool Verbal { get; init; }

        [JsonPropertyName("somatic")]
        public bool Somatic { get; init; }

        [JsonPropertyName("ritual")]
        public bool Ritual { get; init; }

        [JsonPropertyName("concentration")]
        public bool Concentration { get; init; }

        [JsonPropertyName("casting_time")]
        public string? CastingTime { get; init; }

        [JsonPropertyName("duration")]
        public string? Duration { get; init; }

        [JsonPropertyName("school")]
        public Open5eSchoolDto? School { get; init; }

        [JsonPropertyName("level")]
        public int Level { get; init; }

        [JsonPropertyName("classes")]
        public List<Open5eClassDto> Classes { get; init; } = new();

        [JsonPropertyName("shape_size")]
        public int? ShapeSize { get; init; }

        [JsonPropertyName("shape_size_unit")]
        public string? ShapeSizeUnit { get; init; }

        [JsonPropertyName("shape_type")]
        public string? ShapeType { get; init; }

        [JsonPropertyName("material")]
        public bool Material { get; init; }

        [JsonPropertyName("material_specified")]
        public string? MaterialSpecified { get; init; }

        [JsonPropertyName("document")]
        public Open5eDocumentDto? Document { get; init; }
    }

    private sealed class Open5eSpellData
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? HigherLevel { get; init; }
        public string? Range { get; init; }
        public string? Components { get; init; }
        public string? Material { get; init; }
        public string? Ritual { get; init; }
        public string? Concentration { get; init; }
        public string? CastingTime { get; init; }
        public string? Duration { get; init; }
        public string? School { get; init; }
        public string? SchoolKey { get; init; }
        public int LevelInt { get; init; }
        public string? DndClass { get; init; }
        public int? AreaSize { get; init; }
        public string? AreaType { get; init; }
        public string? AreaUnit { get; init; }
        public Open5eDocumentInfo Document { get; init; } = new(null, null, null);
    }

    private sealed record Open5eDocumentInfo(
        string? Key,
        string? Title,
        string? Url);

    private sealed class Open5eSchoolDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("key")]
        public string? Key { get; init; }
    }

    private sealed class Open5eClassDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class Open5eDocumentDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; init; }
    }
}

