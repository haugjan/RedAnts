using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Agents;

public sealed record AgentPayload(
    Hall Hall,
    Game? Game,
    DateTimeOffset SentAt,
    IReadOnlyList<MusicItem>? Music = null,
    TabBoardPayload? Tabs = null);

public sealed record TabChoice(string Id, string Name);

public sealed record TabDeckVariant(string DeckId, string League, string Tab, Deck Layout);

public sealed record TabBoardPayload(
    IReadOnlyList<TabChoice> Leagues,
    IReadOnlyList<TabChoice> Tabs,
    string DefaultLeague,
    IReadOnlyList<TabDeckVariant> Variants);
