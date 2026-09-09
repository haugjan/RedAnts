using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.Helpers;

namespace RedAnts.Ticketing.Tests.Helpers;

internal sealed class RecordingHelpers : IHelperRepository
{
    private int _nextId = 1;

    public List<Helper> Stored { get; } = [];
    public List<(int Id, bool Active)> ActiveChanges { get; } = [];
    public List<(int Id, bool AllEvents, IReadOnlyList<int> EventIds, bool CanRebook)> Assignments { get; } = [];
    public List<int> Deleted { get; } = [];

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

internal sealed class FakeHelperListReader : IHelperListReader
{
    public List<HelperRow> Rows { get; } = [];
    public List<int> Requested { get; } = [];

    public Task<IReadOnlyList<HelperRow>> GetBySeasonAsync(int seasonId)
    {
        Requested.Add(seasonId);
        return Task.FromResult<IReadOnlyList<HelperRow>>(Rows.Where(h => h.SeasonId == seasonId).ToList());
    }
}

internal sealed class FakeHelperScanReportReader : IHelperScanReportReader
{
    public List<HelperScanRow> Rows { get; } = [];
    public List<IReadOnlyCollection<int>> Requested { get; } = [];

    public Task<IReadOnlyList<HelperScanRow>> GetByEventsAsync(IReadOnlyCollection<int> eventIds)
    {
        Requested.Add(eventIds);
        return Task.FromResult<IReadOnlyList<HelperScanRow>>(Rows.Where(r => eventIds.Contains(r.EventId)).ToList());
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
