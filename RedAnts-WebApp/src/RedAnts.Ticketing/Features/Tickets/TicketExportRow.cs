using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record TicketExportRow(
    string CardNo, string? Bundle, string? Category, CardHolder Holder, int? Admissions, string Link);
