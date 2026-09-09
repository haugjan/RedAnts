using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.MemberCards;

internal sealed class InMemoryMemberCards : IMemberCards
{
    public List<MemberCard> Stored { get; } = [];
    public List<MemberCard> Saved { get; } = [];
    public List<(int SeasonId, string Reference, MemberCategory Category, int Rows)> Imports { get; } = [];
    public List<(int SeasonId, MemberCategory Category, string Reference, int Admissions)> Created { get; } = [];

    public Task<int> ImportAsync(int seasonId, string reference, MemberCategory category, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null, string? createdByEmail = null)
    {
        Imports.Add((seasonId, reference, category, rows.Count));
        return Task.FromResult(rows.Count);
    }

    public Task CreateAsync(int seasonId, MemberCategory category, string? firstName, string? lastName, DateOnly? birthday,
        string reference, string? email = null, string? createdByName = null, string? createdByEmail = null,
        MemberAddress? address = null, int admissions = 1)
    {
        Created.Add((seasonId, category, reference, admissions));
        return Task.CompletedTask;
    }

    public Task<bool> ReferenceExistsAsync(int seasonId, string reference) => Task.FromResult(false);

    public Task<IReadOnlyList<string>> GetReferencesAsync() => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<MemberCard>> GetByReferenceAsync(string reference) =>
        Task.FromResult<IReadOnlyList<MemberCard>>(Stored.Where(c => c.Reference == reference).ToList());

    public Task<MemberCard?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(c => c.Uuid == uuid));

    public Task SaveAsync(MemberCard card)
    {
        Saved.Add(card);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingDeletion : IAdminTicketDeletion
{
    public List<(string Kind, Guid Uuid)> Deleted { get; } = [];

    public Task DeleteEventTicketAsync(Guid uuid) => Record("event", uuid);
    public Task DeleteFlexTicketAsync(Guid uuid) => Record("flex", uuid);
    public Task DeleteSeasonPassAsync(Guid uuid) => Record("pass", uuid);
    public Task DeleteMemberCardAsync(Guid uuid) => Record("member", uuid);
    public Task DeleteFreeEntryAsync(Guid uuid) => Record("free", uuid);

    private Task Record(string kind, Guid uuid)
    {
        Deleted.Add((kind, uuid));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingMemberCardMailer : IMemberCardMailer
{
    public List<(MemberCard Card, string Subject, string Body)> Sent { get; } = [];

    public string DefaultSubjectFor(MemberCategory category) => "Betreff";

    public string DefaultBodyFor(MemberCategory category) => "Text";

    public Task<EmailSendResult> SendAsync(MemberCard card, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((card, subject, body));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}
