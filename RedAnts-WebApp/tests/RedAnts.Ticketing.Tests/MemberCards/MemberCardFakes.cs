using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.MemberCards;

internal sealed class InMemoryMemberCards : IMemberCardRepository
{
    public List<MemberCard> Stored { get; } = [];
    public List<MemberCard> Saved { get; } = [];
    public List<MemberCard> Added { get; } = [];
    public List<(int SeasonId, string Reference, MemberCategory Category, int Rows)> Imports { get; } = [];

    public Task<int> ImportAsync(int seasonId, string reference, MemberCategory category, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null, string? createdByEmail = null)
    {
        Imports.Add((seasonId, reference, category, rows.Count));
        return Task.FromResult(rows.Count);
    }

    public Task AddAsync(MemberCard card)
    {
        Added.Add(card);
        Stored.Add(card);
        return Task.CompletedTask;
    }

    public Task<MemberCard?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(c => c.Uuid == uuid));

    public Task SaveAsync(MemberCard card)
    {
        Saved.Add(card);
        return Task.CompletedTask;
    }
}

internal sealed class FakeMemberCardListReader : IMemberCardListReader
{
    public List<MemberCardRow> Rows { get; } = [];
    public List<int> Requested { get; } = [];

    public Task<IReadOnlyList<MemberCardRow>> GetBySeasonAsync(int seasonId)
    {
        Requested.Add(seasonId);
        return Task.FromResult<IReadOnlyList<MemberCardRow>>(Rows);
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

    public string DefaultSubjectFor(MemberCategory category) => $"Betreff {category}";

    public string DefaultBodyFor(MemberCategory category) => $"Text {category}";

    public Task<EmailSendResult> SendAsync(MemberCard card, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((card, subject, body));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}
