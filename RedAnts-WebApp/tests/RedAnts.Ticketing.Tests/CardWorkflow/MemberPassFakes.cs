using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Tests.CardWorkflow;

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

internal sealed class InMemorySeasonPasses : ISeasonPasses
{
    public List<SeasonPass> Stored { get; } = [];
    public List<SeasonPass> Saved { get; } = [];
    public List<(Guid Uuid, CardHolder Holder)> Holders { get; } = [];
    public List<(int SeasonId, int Rows, string Bundle, int? TierId)> Imports { get; } = [];

    public Task<SeasonPass?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(p => p.Uuid == uuid));

    public Task<IReadOnlyList<SeasonPass>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<SeasonPass>>(Stored.Where(p => p.OrderId == orderId).ToList());

    public Task<SeasonPass> SaveAsync(SeasonPass pass)
    {
        Saved.Add(pass);
        return Task.FromResult(pass);
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        Holders.Add((uuid, holder));
        return Task.CompletedTask;
    }

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        int? defaultTierId = null, string? createdByName = null, string? createdByEmail = null)
    {
        Imports.Add((seasonId, rows.Count, defaultBundle, defaultTierId));
        return Task.FromResult((rows.Count, 0));
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

internal sealed class RecordingHelpers : IHelpers
{
    private int _nextId = 1;

    public List<Helper> Stored { get; } = [];
    public List<(int Id, bool Active)> ActiveChanges { get; } = [];
    public List<(int Id, bool AllEvents, IReadOnlyList<int> EventIds, bool CanRebook)> Assignments { get; } = [];
    public List<int> Deleted { get; } = [];

    public Task<IReadOnlyList<Helper>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<Helper>>(Stored.Where(h => h.SeasonId == seasonId).ToList());

    public Task<Helper?> FindByIdAsync(int id) => Task.FromResult(Stored.FirstOrDefault(h => h.Id == id));

    public Task<Helper?> FindByPasswordAsync(string code) => Task.FromResult(Stored.FirstOrDefault(h => h.Code == code));

    public Task<Helper> AddAsync(int seasonId, string firstName, string lastName, string email)
    {
        var created = Helper.Create(seasonId, firstName, lastName, email, $"code-{_nextId}");
        var helper = Helper.FromPersistence(_nextId++, created.SeasonId, created.FirstName, created.LastName, created.Email,
            created.Code, created.AllEvents, created.EventIds, created.CanRebook, created.Active, created.CreatedAt);
        Stored.Add(helper);
        return Task.FromResult(helper);
    }

    public Task SetActiveAsync(int id, bool active)
    {
        ActiveChanges.Add((id, active));
        return Task.CompletedTask;
    }

    public Task SetAssignmentAsync(int id, bool allEvents, IReadOnlyList<int> eventIds, bool canRebook)
    {
        Assignments.Add((id, allEvents, eventIds, canRebook));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        Deleted.Add(id);
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

internal sealed class RecordingSeasonPassMailer : ISeasonPassMailer
{
    public List<(SeasonPass Pass, string CategoryLabel, string Subject)> Sent { get; } = [];

    public string DefaultSubject => "Betreff";

    public string DefaultBody => "Text";

    public Task<EmailSendResult> SendAsync(SeasonPass pass, string categoryLabel, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        Sent.Add((pass, categoryLabel, subject));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}

internal sealed class RecordingHelperInviteMailer : IHelperInviteMailer
{
    public List<(Helper Helper, string Subject, string LoginLink)> Sent { get; } = [];

    public string DefaultSubject => "Betreff";

    public string DefaultBody => "Text";

    public Task<EmailSendResult> SendAsync(Helper helper, string subject, string body, string loginLink,
        CancellationToken cancellationToken = default)
    {
        Sent.Add((helper, subject, loginLink));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}
