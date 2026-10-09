using RedAnts.Game.Domain;
using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Squad;

public sealed record SquadSlotView(
    int Slot,
    int Line,
    PlayerPosition Role,
    string Label,
    PlayerCardView? Player);

public sealed record SquadLineView(int Line, string Label, IReadOnlyList<SquadSlotView> Slots);

public sealed record SquadView(
    string Name,
    int Budget,
    int Spent,
    int Placed,
    IReadOnlyList<SquadLineView> Lines)
{
    public int Free => Budget - Spent;
    public int Total => SquadLayout.Slots;
    public bool IsComplete => Placed == SquadLayout.Slots;
}
