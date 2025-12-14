using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dnd35.SpellCards.Rendering;

public static class SchoolTheme
{
    public static string Color(string schoolKey) => schoolKey.ToLowerInvariant() switch
    {
        "abjuration" => "#3A6EA5",
        "conjuration" => "#3A8F5C",
        "divination" => "#C9A227",
        "enchantment" => "#7A4FA3",
        "evocation" => "#B22222",
        "illusion" => "#4B5FA5",
        "necromancy" => "#444444",
        "transmutation" => "#C6862C",
        _ => Colors.Black
    };

    public static string IconPath(string schoolKey) =>
        Path.Combine(AppContext.BaseDirectory, $"assets/icons/{schoolKey}.svg");

}
