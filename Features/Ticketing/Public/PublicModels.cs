using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Public;

public sealed class EventPurchaseModel
{
    public required Event Event { get; init; }
    public required string Sqid { get; init; }
    public string? Secret { get; init; }
    public required string SeasonName { get; init; }
    public required Venue? Venue { get; init; }
    public string? TurnstileSiteKey { get; init; }
    public string? Error { get; init; }
}

public sealed class SeasonPurchaseModel
{
    public required Season Season { get; init; }
    public required string Sqid { get; init; }
    public string? Secret { get; init; }
    public required IReadOnlyList<(SeasonTicketCategory Category, AgeGroup AgeGroup, decimal Amount)> Prices { get; init; }
    public string? TurnstileSiteKey { get; init; }
    public string? Error { get; init; }
}

public sealed class ConfirmationModel
{
    public required bool Found { get; init; }
    public required bool Paid { get; init; }
    public required string Title { get; init; }
    public string Summary { get; init; } = "";
    public Guid TicketRef { get; init; }
}
