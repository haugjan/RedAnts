using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record TicketImportRow(
    string? CardNo, string? Bundle, string? Category, int? Admissions, CardHolder Holder);
