using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Write side for the Mitglieder admin tab: edits the details of a single member card. Kept in
/// the admin slice and writing only the editable columns, so it is independent of the member-card
/// repository/record (which is mid-migration around its columns).</summary>
public interface IMemberCardEditor
{
    Task SetDetailsAsync(Guid uuid, string? firstName, string? lastName, DateOnly? birthday,
        MemberCategory category, TicketStatus status, string? reference);
}
