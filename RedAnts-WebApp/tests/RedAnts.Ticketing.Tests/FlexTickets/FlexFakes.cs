using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.FlexTickets;

internal sealed class RecordingFlexBundles : IFlexTicketBundleRepository
{
    public Dictionary<int, FlexTicketBundle> Bundles { get; } = new();
    public HashSet<string> Existing { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<FlexTicketBundle> Saved { get; } = [];
    public List<string> Calls { get; } = [];
    public List<(int SeasonId, string Reference, int Quantity)> Created { get; } = [];

    public Task<FlexRebookResult> RebookByUuidAsync(int targetBundleId, Guid uuid, string? operatorName)
    {
        Calls.Add($"rebook-uuid:{targetBundleId}:{uuid}");
        return Task.FromResult(new FlexRebookResult(FlexRebookStatus.Moved));
    }

    public Task<FlexRebookResult> RebookByCodeAsync(int targetBundleId, string codePrefix, string? operatorName)
    {
        Calls.Add($"rebook-code:{targetBundleId}:{codePrefix}");
        return Task.FromResult(new FlexRebookResult(FlexRebookStatus.Moved));
    }

    public Task<FlexBoxOfficeResult> ConvertToBoxOfficeByUuidAsync(Guid uuid, string? operatorName)
    {
        Calls.Add($"boxoffice-uuid:{uuid}");
        return Task.FromResult(new FlexBoxOfficeResult(FlexBoxOfficeStatus.Converted));
    }

    public Task<FlexBoxOfficeResult> ConvertToBoxOfficeByCodeAsync(string codePrefix, string? operatorName)
    {
        Calls.Add($"boxoffice-code:{codePrefix}");
        return Task.FromResult(new FlexBoxOfficeResult(FlexBoxOfficeStatus.Converted));
    }

    public Task SetTicketStatusAsync(Guid uuid, TicketStatus status)
    {
        Calls.Add($"status:{uuid}:{status}");
        return Task.CompletedTask;
    }

    public Task SetTicketRedeemedAsync(Guid uuid, bool redeemed)
    {
        Calls.Add($"redeemed:{uuid}:{redeemed}");
        return Task.CompletedTask;
    }

    public Task SetTicketCategoryAsync(Guid uuid, TicketCategory category)
    {
        Calls.Add($"category:{uuid}:{category}");
        return Task.CompletedTask;
    }

    public Task<bool> ReferenceExistsAsync(int seasonId, string reference) => Task.FromResult(Existing.Contains(reference));

    public Task<int> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        Created.Add((seasonId, reference, quantity));
        return Task.FromResult(Created.Count);
    }

    public Task<int> AddTicketsAsync(int bundleId, TicketCategory category, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        Calls.Add($"add:{bundleId}:{quantity}");
        return Task.FromResult(bundleId);
    }

    public Task<int> CreateEmptyAsync(int seasonId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null)
    {
        Created.Add((seasonId, reference, 0));
        return Task.FromResult(Created.Count);
    }

    public Task<bool> DeleteEmptyAsync(int bundleId)
    {
        Calls.Add($"delete-empty:{bundleId}");
        return Task.FromResult(true);
    }

    public bool ImportThrows { get; set; }

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        TicketCategory defaultCategory, string? createdByName = null, string? createdByEmail = null)
    {
        if (ImportThrows) throw new InvalidOperationException("flex ticket table unavailable");
        Calls.Add($"import:{seasonId}:{defaultBundle}");
        return Task.FromResult((rows.Count, 0));
    }

    public Task<Guid> CreateSingleAsync(int seasonId, TicketCategory category, string reference, CardHolder holder,
        string? createdByName = null, string? createdByEmail = null)
    {
        Calls.Add($"single:{seasonId}:{reference}");
        return Task.FromResult(Guid.NewGuid());
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        Calls.Add($"holder:{uuid}");
        return Task.CompletedTask;
    }

    public Task<FlexTicketBundle?> GetByIdAsync(int bundleId) =>
        Task.FromResult(Bundles.TryGetValue(bundleId, out var bundle) ? bundle : null);

    public Task SaveAsync(FlexTicketBundle bundle)
    {
        Saved.Add(bundle);
        return Task.CompletedTask;
    }
}

internal sealed class FakeFlexBundleListReader : IFlexBundleListReader
{
    public List<FlexBundleRow> Rows { get; } = [];
    public List<int> Requested { get; } = [];

    public Task<IReadOnlyList<FlexBundleRow>> GetBySeasonAsync(int seasonId)
    {
        Requested.Add(seasonId);
        return Task.FromResult<IReadOnlyList<FlexBundleRow>>(Rows.Where(b => b.SeasonId == seasonId).ToList());
    }
}

internal sealed class FakeFlexBundleTicketsReader : IFlexBundleTicketsReader
{
    public Dictionary<int, List<FlexTicketRow>> ByBundle { get; } = new();
    public List<string> Calls { get; } = [];

    public Task<IReadOnlyList<FlexTicketRow>> GetByBundleAsync(int bundleId)
    {
        Calls.Add($"bundle:{bundleId}");
        return Task.FromResult<IReadOnlyList<FlexTicketRow>>(ByBundle.GetValueOrDefault(bundleId) ?? []);
    }

    public Task<IReadOnlyList<FlexTicketRow>> GetBySeasonAsync(int seasonId)
    {
        Calls.Add($"season:{seasonId}");
        return Task.FromResult<IReadOnlyList<FlexTicketRow>>(ByBundle.Values.SelectMany(t => t).Where(t => t.SeasonId == seasonId).ToList());
    }

    public Task<IReadOnlyList<FlexTicketRow>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds)
    {
        Calls.Add($"bundles:{string.Join(',', bundleIds)}");
        return Task.FromResult<IReadOnlyList<FlexTicketRow>>(bundleIds.SelectMany(id => ByBundle.GetValueOrDefault(id) ?? []).ToList());
    }
}

internal sealed class RecordingFlexMailer : IFlexTicketMailer
{
    public List<(FlexMailTicket Ticket, string Subject)> Sent { get; } = [];
    public string DefaultSubject => "Dein Flexticket";
    public string DefaultBody => "Hallo";

    public Task<EmailSendResult> SendAsync(FlexMailTicket ticket, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((ticket, subject));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}
