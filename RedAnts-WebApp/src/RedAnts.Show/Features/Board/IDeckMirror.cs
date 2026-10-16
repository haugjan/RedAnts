using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public sealed record DeckKeyRow(
    int Number,
    string Label,
    string? Icon,
    string Color,
    string TextColor,
    bool Pressable,
    string? MusicId = null,
    bool Active = false,
    string Symbol = "",
    string Subtitle = "");

public sealed record DeckLive(
    string Time,
    string PeriodLabel,
    string HomeScore,
    string AwayScore,
    bool ClockLive,
    string MusicId = "",
    long MusicOrder = 0,
    string Control = "",
    long ControlOrder = 0);

public sealed record DeckView(
    string DeckId,
    string Title,
    int Columns,
    int Rows,
    IReadOnlyList<DeckKeyRow> Keys,
    bool FromAgent,
    DeckLive? Live = null,
    IReadOnlyDictionary<string, string>? States = null);

public interface IDeckMirror
{
    bool IsConnected(string hallId);

    DeckView? Known(string hallId, string deckId);

    Task<DeckView?> LiveAsync(string hallId, Deck deck);

    Task<bool> PressAsync(string hallId, string deckId, int number);

    Task<bool> MusicAsync(string hallId, string musicId);

    void Apply(string hallId, string deckId, string json, Deck deck);

    void ApplyLive(string hallId, string json);

    void Drop(string hallId);
}
