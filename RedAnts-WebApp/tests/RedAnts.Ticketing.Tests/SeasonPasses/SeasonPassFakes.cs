using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.SeasonPasses;

internal sealed class InMemorySeasonPasses : ISeasonPassRepository
{
    public List<SeasonPass> Stored { get; } = [];
    public List<SeasonPass> Saved { get; } = [];
    public List<(Guid Uuid, CardHolder Holder)> Holders { get; } = [];
    public List<(int SeasonId, int Rows, string Bundle, int? TierId)> Imports { get; } = [];

    public Task<SeasonPass?> GetByUuidAsync(Guid uuid) => Task.FromResult(Stored.FirstOrDefault(p => p.Uuid == uuid));

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

    public bool ImportThrows { get; set; }

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        int? defaultTierId = null, string? createdByName = null, string? createdByEmail = null)
    {
        if (ImportThrows) throw new InvalidOperationException("season pass table unavailable");
        Imports.Add((seasonId, rows.Count, defaultBundle, defaultTierId));
        return Task.FromResult((rows.Count, 0));
    }
}

internal sealed class FakeSeasonPassListReader : ISeasonPassListReader
{
    public List<SeasonPassRow> Rows { get; } = [];
    public List<int> Requested { get; } = [];

    public Task<IReadOnlyList<SeasonPassRow>> GetBySeasonAsync(int seasonId)
    {
        Requested.Add(seasonId);
        return Task.FromResult<IReadOnlyList<SeasonPassRow>>(Rows);
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
