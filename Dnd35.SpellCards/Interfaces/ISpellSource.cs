using Dnd35.SpellCards.Models;

namespace Dnd35.SpellCards.Sources;

public interface ISpellSource
{
    Task<IReadOnlyList<Spell>> FetchSpellsAsync(IReadOnlyList<SpellRequest> requests, CancellationToken ct);
}
