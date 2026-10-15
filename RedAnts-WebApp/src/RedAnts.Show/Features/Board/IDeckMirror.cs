using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public sealed record DeckKeyRow(int Number, string Label, string? Icon, string Color, string TextColor, bool Pressable, string? SoundId);

public sealed record DeckLive(string Time, string PeriodLabel, string HomeScore, string AwayScore, bool ClockLive, string SoundId, long SoundOrder);

public sealed record DeckView(
    string DeckId,
    string Title,
    int Columns,
    IReadOnlyList<DeckKeyRow> Keys,
    bool FromAgent,
    DeckLive? Live = null,
    IReadOnlyDictionary<string, string>? States = null);

public interface IDeckMirror
{
    bool IsConnected(string hallId);

    DeckView? Known(string hallId, string deckId);

    Task<DeckView?> LiveAsync(string hallId, DeckLayout layout);

    Task<bool> PressAsync(string hallId, string deckId, int number);

    Task<bool> SoundAsync(string hallId, string soundId);

    void Apply(string hallId, string deckId, string json, DeckLayout layout);

    void ApplyLive(string hallId, string json);

    void Drop(string hallId);
}
