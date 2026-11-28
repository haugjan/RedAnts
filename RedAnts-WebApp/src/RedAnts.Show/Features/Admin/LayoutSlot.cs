using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public enum LayoutSlotKind
{
    Free,
    Reserved,
    Placed,
    Run,
    Always,
}

public sealed record LayoutSlot(
    int Number,
    LayoutSlotKind Kind,
    BoardButton? Button = null,
    string? Caption = null,
    int? Position = null,
    string? RunId = null);
