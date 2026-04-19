using System.Text.Json;
using SpellCards.Infrastructure;
using SpellCards.Models;

namespace SpellCards.Sources;

public sealed class CustomSpellSource
{
    private readonly string _path;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public CustomSpellSource(string path)
    {
        _path = path;
    }

    internal IReadOnlyDictionary<string, Spell> Resolve(global::RuleSet ruleSet, IReadOnlyList<string> requestedNames)
    {
        if (!File.Exists(_path) || requestedNames.Count == 0)
            return new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(_path);
        var definitions = JsonSerializer.Deserialize<List<CustomSpellDefinition>>(json, SerializerOptions) ?? [];

        var available = definitions
            .Where(d => MatchesRuleSet(d.RuleSet, ruleSet))
            .Where(d => !string.IsNullOrWhiteSpace(d.Name))
            .Select(MapToSpell)
            .ToDictionary(spell => spell.Name, spell => spell, StringComparer.OrdinalIgnoreCase);

        var resolved = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
        if (available.Count == 0)
            return resolved;

        var nameIndex = available.Keys.ToDictionary(name => name, _ => "custom", StringComparer.OrdinalIgnoreCase);

        foreach (var requestedName in requestedNames)
        {
            try
            {
                var resolvedName = SpellNameResolver.Resolve(requestedName, nameIndex);
                resolved[requestedName] = available[resolvedName];
            }
            catch
            {
            }
        }

        return resolved;
    }

    private static bool MatchesRuleSet(string? rawRuleSet, global::RuleSet ruleSet)
    {
        if (string.IsNullOrWhiteSpace(rawRuleSet))
            return true;

        var normalized = rawRuleSet.Trim().ToLowerInvariant();
        return ruleSet switch
        {
            global::RuleSet.Dnd35 => normalized is "3.5" or "3.5e" or "35" or "dnd35" or "dnd3.5" or "any" or "all",
            global::RuleSet.Dnd5e => normalized is "5e" or "5.0" or "5" or "dnd5" or "dnd5e" or "dnd5.0" or "any" or "all",
            global::RuleSet.Dnd55 => normalized is "5.5" or "5.5e" or "55" or "dnd55" or "dnd5.5" or "dnd2024" or "2024" or "5e2024" or "any" or "all",
            global::RuleSet.Pf1 => normalized is "pf" or "pf1" or "pf1e" or "pathfinder" or "pathfinder1" or "pathfinder1e" or "any" or "all",
            global::RuleSet.Pf2 => normalized is "pf2" or "pf2e" or "pathfinder2" or "pathfinder2e" or "any" or "all",
            _ => false
        };
    }

    private static Spell MapToSpell(CustomSpellDefinition definition)
    {
        return new Spell
        {
            Name = definition.Name!.Trim(),
            Part = string.Empty,
            ClassLevel = definition.ClassLevel?.Trim() ?? string.Empty,
            SchoolText = definition.SchoolText?.Trim() ?? string.Empty,
            SchoolKey = definition.SchoolKey?.Trim() ?? string.Empty,
            Cast = definition.Cast?.Trim() ?? string.Empty,
            Range = definition.Range?.Trim() ?? string.Empty,
            TargetOrArea = definition.TargetOrArea?.Trim() ?? string.Empty,
            Duration = definition.Duration?.Trim() ?? string.Empty,
            Save = definition.Save?.Trim() ?? string.Empty,
            Sr = definition.Sr?.Trim() ?? string.Empty,
            Components = definition.Components?.Trim() ?? string.Empty,
            Tags = definition.Tags?.Trim() ?? string.Empty,
            Description = definition.Description?.Trim() ?? string.Empty,
            Notes = definition.Notes?.Trim() ?? string.Empty,
            SourceUrl = definition.SourceUrl?.Trim() ?? string.Empty
        };
    }

    private sealed class CustomSpellDefinition
    {
        public string? RuleSet { get; init; }
        public string? Name { get; init; }
        public string? ClassLevel { get; init; }
        public string? SchoolText { get; init; }
        public string? SchoolKey { get; init; }
        public string? Cast { get; init; }
        public string? Range { get; init; }
        public string? TargetOrArea { get; init; }
        public string? Duration { get; init; }
        public string? Save { get; init; }
        public string? Sr { get; init; }
        public string? Components { get; init; }
        public string? Tags { get; init; }
        public string? Description { get; init; }
        public string? Notes { get; init; }
        public string? SourceUrl { get; init; }
    }
}
