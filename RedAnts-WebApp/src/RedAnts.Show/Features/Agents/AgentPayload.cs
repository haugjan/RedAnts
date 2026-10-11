using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Agents;

public sealed record AgentPayload(
    Hall Hall,
    IReadOnlyList<Scene> Scenes,
    Game? Game,
    DateTimeOffset SentAt,
    IReadOnlyList<SoundCommand>? Sounds = null,
    IReadOnlyList<ShowState>? States = null);
