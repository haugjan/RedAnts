using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record TicketImportRow(
    string? CardNo, string? Bundle, string? Category, int? Admissions, CardHolder Holder);
