using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Newsletter.Infrastructure;

public sealed class NewsletterSignupListReader(IScopeProvider scopeProvider) : INewsletterSignupListReader
{
    private const string Columns = "SELECT Id, Email, Name, Source, SignedUpAt, Status, TransferredAt FROM NewsletterSignups";

    public async Task<IReadOnlyList<NewsletterSignupRow>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<NewsletterSignupRecord>(Columns + " ORDER BY SignedUpAt DESC");
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<NewsletterSignupRow>> GetPendingAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<NewsletterSignupRecord>(
            Columns + " WHERE Status = @0 ORDER BY SignedUpAt", (int)NewsletterTransferStatus.Pending);
        return rows.Select(Map).ToList();
    }

    private static NewsletterSignupRow Map(NewsletterSignupRecord r) =>
        new(r.Id, r.Email, r.Name, r.Source, r.SignedUpAt, (NewsletterTransferStatus)r.Status, r.TransferredAt);
}
