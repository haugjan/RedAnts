namespace RedAnts.Show.Domain;

public sealed record ShowSetup(
    IReadOnlyList<OverlayTitle> Titles,
    IReadOnlyList<LedTemplate> Led,
    IReadOnlyList<Deck>? Decks = null)
{
    public static ShowSetup Default => new(Hall.DefaultOverlayTitles, Hall.DefaultLedTemplates, DefaultDecks.All);

    public static readonly ShowSetup Empty = new([], []);

    public IReadOnlyList<Deck> DeckLibrary => Decks is { Count: > 0 } decks ? decks : DefaultDecks.All;

    public bool IsEmpty => Titles.Count == 0 && Led.Count == 0;

    public IReadOnlyList<Deck> DecksOf(Hall hall) =>
        hall.DeckIds is { Count: > 0 } wanted
            ? DeckLibrary.Where(deck => wanted.Contains(deck.Id, StringComparer.OrdinalIgnoreCase)).ToList()
            : DeckLibrary;
}
