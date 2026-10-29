namespace RedAnts.Show.Domain;

public sealed record DeckFormat(string Id, string Name, int Columns, int Rows, int PanicKey, int BackKey)
{
    public const string Mk2 = "mk2";
    public const string Xl = "xl";

    public static IReadOnlyList<DeckFormat> All { get; } =
    [
        new(Mk2, "Streamdeck Mk2", 5, 3, 11, 1),
        new(Xl, "Streamdeck XL", 8, 4, 32, 25),
    ];

    public static DeckFormat Default => All[0];

    public int Count => Columns * Rows;

    public string Label => $"{Name} ({Columns} × {Rows} Tasten)";

    public bool Fits(Deck deck) => deck.Columns == Columns && deck.Rows == Rows;

    public static DeckFormat? Of(Deck deck) => All.FirstOrDefault(format => format.Fits(deck));

    public static DeckFormat? Of(string? id) => All.FirstOrDefault(format => format.Id == id);

    public static string NameOf(Deck deck) => Of(deck)?.Label ?? $"{deck.Columns} × {deck.Rows} Tasten";

    public Deck Create(string id, string name) =>
        new(id, name, Columns, Rows, [Deck.Empty(DeckPage.HomeId, "Startseite")], PanicKey, BackKey);
}
