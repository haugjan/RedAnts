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
                || b.Id.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(b => b.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsColor(string? color) => color is not null && HexColor().IsMatch(color);

    public static BoardButton Normalized(BoardButton button) => button with
    {
        Label = button.Label.Trim(),
        Leagues = button.Leagues is { Count: > 0 } leagues ? leagues : null,
        PerLeague = button.PerLeague is { Count: > 0 } variants ? variants : null,
        State = button.State is { Length: > 0 } state ? state : null,
    };

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
