namespace RedAnts.Show.Domain;

public sealed record ShowSetup(
    IReadOnlyList<OverlayTitle> Titles,
    IReadOnlyList<LedTemplate> Led,
    IReadOnlyList<Deck>? Decks = null,
    IReadOnlyList<PageSet>? Sheets = null)
{
    public static ShowSetup Default => new(Hall.DefaultOverlayTitles, Hall.DefaultLedTemplates, DefaultDecks.All);

    public static readonly ShowSetup Empty = new([], []);

    public IReadOnlyList<Deck> DeckLibrary => Decks is { Count: > 0 } decks ? decks : DefaultDecks.All;

    public IReadOnlyList<PageSet> PageLibrary => Sheets ?? [];

    public bool IsEmpty => Titles.Count == 0 && Led.Count == 0;

    public IReadOnlyList<DeckPage> PagesOf(Deck deck)
    {
        if (PageLibrary.FirstOrDefault(set => set.Fits(deck)) is not { Pages.Count: > 0 } shared) return deck.Pages;
        var own = deck.Pages.Select(page => page.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. deck.Pages, .. shared.Pages.Where(page => !page.IsHome && !own.Contains(page.Id))];
    }

    public Deck Complete(Deck deck) => deck with { Pages = PagesOf(deck) };

    public IReadOnlyList<Deck> DecksOf(Hall hall) =>
        (hall.DeckIds is { Count: > 0 } wanted
            ? DeckLibrary.Where(deck => wanted.Contains(deck.Id, StringComparer.OrdinalIgnoreCase))
            : DeckLibrary)
        .Select(Complete)
        .ToList();
}
