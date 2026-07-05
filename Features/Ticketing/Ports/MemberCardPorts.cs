using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record MemberImportRow(MemberCategory Category, string? LastName, string? FirstName, DateOnly? Birthday);

public interface IMemberCards
{
    Task<int> ImportAsync(int seasonId, string reference, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null);
}
