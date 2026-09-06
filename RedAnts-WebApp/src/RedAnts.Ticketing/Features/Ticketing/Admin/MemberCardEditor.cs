using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public interface IMemberCardEditor
{
    Task SetDetailsAsync(Guid uuid, string? firstName, string? lastName, DateOnly? birthday,
        MemberCategory category, TicketStatus status, string? reference, string? email = null,
        MemberAddress? address = null, int admissions = 1);

    Task SetStatusAsync(Guid uuid, TicketStatus status);
    Task SetCategoryAsync(Guid uuid, MemberCategory category);
}
