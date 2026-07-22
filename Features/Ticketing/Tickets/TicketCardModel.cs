namespace RedAnts.Features.Ticketing.Tickets;

public sealed record TicketCardModel(
    string Kicker,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string Serial,
    string QrMarkup,
    bool Invalid = false,
    string? ScanText = null,
    string? VenueName = null);
