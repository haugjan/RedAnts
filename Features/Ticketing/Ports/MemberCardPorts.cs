using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record MemberImportRow(string? LastName, string? FirstName, DateOnly? Birthday);

public interface IMemberCards
{
    Task<int> ImportAsync(int seasonId, MemberCategory category, string? reference, IReadOnlyList<MemberImportRow> rows);
}
