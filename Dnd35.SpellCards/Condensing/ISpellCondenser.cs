using Dnd35.SpellCards.Models;

namespace Dnd35.SpellCards.Condensing;

public interface ISpellCondenser
{
    Task<string> CondenseAsync(Spell spell, CancellationToken ct);
}
