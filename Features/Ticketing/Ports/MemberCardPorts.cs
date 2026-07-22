using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record MemberImportRow(MemberCategory Category, string? LastName, string? FirstName, DateOnly? Birthday);

public interface IMemberCards
{
    Task<int> ImportAsync(int seasonId, string reference, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null, string? createdByEmail = null);

    Task CreateAsync(int seasonId, MemberCategory category, string? firstName, string? lastName,
        DateOnly? birthday, string reference, string? createdByName = null, string? createdByEmail = null);

    Task<bool> ReferenceExistsAsync(int seasonId, string reference);

    Task<IReadOnlyList<string>> GetReferencesAsync();

    Task<IReadOnlyList<MemberCard>> GetByReferenceAsync(string reference);
}
