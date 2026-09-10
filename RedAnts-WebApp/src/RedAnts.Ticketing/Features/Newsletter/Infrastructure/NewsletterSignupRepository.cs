using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Newsletter.Infrastructure;

public sealed class NewsletterSignupRepository(IScopeProvider scopeProvider) : INewsletterSignupRepository
{
    public async Task SubscribeAsync(string email, string? name, string source)
    {
        var signup = NewsletterSignup.Create(email, name, source);
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var existing = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM NewsletterSignups WHERE Email = @0", signup.Email.Value);
        if (existing > 0) return;
        await scope.Database.InsertAsync(new NewsletterSignupRecord
        {
            Email = signup.Email.Value,
            Name = signup.Name,
            Source = signup.Source,
            SignedUpAt = signup.SignedUpAt,
            Status = (int)signup.Status,
            TransferredAt = signup.TransferredAt
        });
    }

    public async Task MarkTransferredAsync(IEnumerable<int> ids)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return;
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE NewsletterSignups SET Status = @0, TransferredAt = @1 WHERE Status = @2 AND Id IN (@3)",
            (int)NewsletterTransferStatus.Transferred, SwissTime.Timestamp, (int)NewsletterTransferStatus.Pending, list);
    }

    public async Task SetTransferStatusAsync(int id, NewsletterTransferStatus status)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.SingleOrDefaultByIdAsync<NewsletterSignupRecord>(id);
        if (row is null) return;
        var signup = NewsletterSignup.FromPersistence(row.Id, row.Email, row.Name, row.Source, row.SignedUpAt, row.Status, row.TransferredAt);
        if (status == NewsletterTransferStatus.Transferred) signup.MarkTransferred(SwissTime.Timestamp);
        else signup.MarkPending();
        row.Status = (int)signup.Status;
        row.TransferredAt = signup.TransferredAt;
        await scope.Database.UpdateAsync(row);
    }
}
