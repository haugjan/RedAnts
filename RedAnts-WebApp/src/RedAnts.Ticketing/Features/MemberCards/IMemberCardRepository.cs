using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.MemberCards;

public sealed record MemberImportRow(string? LastName, string? FirstName,
    DateOnly? Birthday, string? Email = null, string? CardNo = null, MemberAddress? Address = null,
    int Admissions = 1, string? Reference = null);

public interface IMemberCardRepository
{
    Task<MemberCard?> GetByUuidAsync(Guid uuid);
    Task AddAsync(MemberCard card);
    Task SaveAsync(MemberCard card);
    Task<int> ImportAsync(int seasonId, string reference, MemberCategory category, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null, string? createdByEmail = null);
}
