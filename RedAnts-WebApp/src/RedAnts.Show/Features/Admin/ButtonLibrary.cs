using System.Text.RegularExpressions;
using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public static partial class ButtonLibrary
{
    public static IReadOnlyList<BoardButton> Find(IEnumerable<BoardButton> buttons, string? search)
    {
        var text = search?.Trim() ?? "";
        return buttons
            .Where(b => text.Length == 0
                || b.Label.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || b.Id.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (b.Category ?? "").Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(b => b.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(b => b.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public const string Uncategorized = "Ohne Kategorie";

    public static readonly IReadOnlyList<string> Order =
    [
        "Vor dem Spiel", "Während dem Spiel", "Spielerinnen", "Pause", "Nach dem Spiel", "Spezielle Situationen",
        "Musik", "Musiksteuerung", "vMix", "LED", "Teamlogos", "Meldungen", "Anzeige", "Funktionen",
    ];

    public static string CategoryOf(BoardButton button) =>
        button.Category is { Length: > 0 } category ? category : Uncategorized;

    public static IReadOnlyList<IGrouping<string, BoardButton>> Grouped(IEnumerable<BoardButton> buttons, string? search) =>
        Find(buttons, search)
            .GroupBy(CategoryOf)
            .OrderBy(g => Rank(g.Key))
            .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> Categories(IEnumerable<BoardButton> buttons) =>
        Order.Concat(buttons.Select(b => b.Category).OfType<string>().Where(c => c.Length > 0))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static int Rank(string category) =>
        category == Uncategorized ? int.MaxValue
        : Order.IndexOf(category) is var index && index >= 0 ? index
        : Order.Count;

    public static bool IsColor(string? color) => color is not null && HexColor().IsMatch(color);

    public static BoardButton Normalized(BoardButton button) => button with
    {
        Label = button.Label.Trim(),
        Leagues = button.Leagues is { Count: > 0 } leagues ? leagues : null,
        PerLeague = button.PerLeague is { Count: > 0 } variants ? variants : null,
        State = button.State is { Length: > 0 } state ? state : null,
        Category = button.Category?.Trim() is { Length: > 0 } category ? category : null,
    };

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
