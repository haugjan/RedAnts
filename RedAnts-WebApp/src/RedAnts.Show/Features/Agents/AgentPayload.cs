using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Agents;

public sealed record AgentPayload(
    Hall Hall,
    Game? Game,
    DateTimeOffset SentAt,
    IReadOnlyList<MusicItem>? Music = null);
