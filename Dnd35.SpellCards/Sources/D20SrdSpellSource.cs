using Dnd35.SpellCards.Infrastructure;
using Dnd35.SpellCards.Models;
using Dnd35.SpellCards.Parsing;

namespace Dnd35.SpellCards.Sources;

public sealed class D20SrdSpellSource
{
    private const string BaseUrl = "https://www.d20srd.org/";
    private const string SpellIndexUrl = "https://www.d20srd.org/indexes/spells.htm";

    private readonly HttpCache _cache;

    public D20SrdSpellSource(HttpCache cache) => _cache = cache;

    public async Task<IReadOnlyList<Spell>> FetchSpellsAsync(IReadOnlyList<string> spellNames, CancellationToken ct)
    {
        var indexHtml = await _cache.GetStringCachedAsync(SpellIndexUrl, ct);
        var nameToUrl = D20SrdIndexParser.ParseNameToUrl(indexHtml, BaseUrl);

        var result = new List<Spell>();

        foreach (var name in spellNames)
        {
            var resolvedName = SpellNameResolver.Resolve(name, nameToUrl);
            var url = nameToUrl[resolvedName];

            var html = await _cache.GetStringCachedAsync(url, ct);
            var spell = D20SrdSpellParser.ParseSpellPage(html, url);
            result.Add(spell);
        }

        return result;
    }
}
