using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class NewsletterSignupRepository(IScopeProvider scopeProvider) : INewsletterSignups
{
    public async Task SubscribeAsync(string email, string? name, string source)
    {
        var signup = NewsletterSignup.Create(email, name, source);
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var existing = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM NewsletterSignups WHERE Email = @0", signup.Email);
        if (existing > 0) return;
        await scope.Database.InsertAsync(new NewsletterSignupRecord
        {
            Email = signup.Email,
            Name = signup.Name,
            Source = signup.Source,
            SignedUpAt = signup.SignedUpAt,
            Status = (int)signup.Status,
            TransferredAt = signup.TransferredAt
        });
    }

    public async Task<IReadOnlyList<NewsletterSignup>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<NewsletterSignupRecord>(
            "SELECT Id, Email, Name, Source, SignedUpAt, Status, TransferredAt FROM NewsletterSignups ORDER BY SignedUpAt DESC");
        return rows.Select(Map).ToList();
    }

    public async Task SetTransferStatusAsync(int id, NewsletterTransferStatus status)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.SingleOrDefaultByIdAsync<NewsletterSignupRecord>(id);
        if (row is null) return;
        var signup = Map(row);
        if (status == NewsletterTransferStatus.Transferred) signup.MarkTransferred(DateTime.UtcNow);
        else signup.MarkPending();
        row.Status = (int)signup.Status;
        row.TransferredAt = signup.TransferredAt;
        await scope.Database.UpdateAsync(row);
    }

    private static NewsletterSignup Map(NewsletterSignupRecord r) =>
        NewsletterSignup.FromPersistence(r.Id, r.Email, r.Name, r.Source, r.SignedUpAt, r.Status, r.TransferredAt);
}
