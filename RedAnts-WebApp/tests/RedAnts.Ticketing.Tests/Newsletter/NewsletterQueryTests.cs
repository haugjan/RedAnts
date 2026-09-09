using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Newsletter;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Newsletter;

internal sealed class StubNewsletterSignupList : INewsletterSignupListReader
{
    public List<NewsletterSignupRow> Rows { get; } = [];

    public Task<IReadOnlyList<NewsletterSignupRow>> GetAllAsync() => Task.FromResult<IReadOnlyList<NewsletterSignupRow>>(Rows);

    public Task<IReadOnlyList<NewsletterSignupRow>> GetPendingAsync() =>
        Task.FromResult<IReadOnlyList<NewsletterSignupRow>>(Rows.Where(r => r.Status == NewsletterTransferStatus.Pending).ToList());

    public static NewsletterSignupRow Row(int id, NewsletterTransferStatus status) =>
        new(id, $"person{id}@example.ch", $"Person {id}", "Checkout", SwissTime.Timestamp, status, null);
}

public class GetNewsletterSignupsTests
{
    [Fact]
    public async Task Lists_all_signups_or_only_the_pending_ones()
    {
        var reader = new StubNewsletterSignupList();
        reader.Rows.Add(StubNewsletterSignupList.Row(1, NewsletterTransferStatus.Transferred));
        reader.Rows.Add(StubNewsletterSignupList.Row(2, NewsletterTransferStatus.Pending));
        var handler = new GetNewsletterSignups.Handler(reader);

        var all = await handler.HandleAsync(new GetNewsletterSignups.Query());
        var pending = await handler.HandleAsync(new GetNewsletterSignups.Query(OnlyPending: true));

        Assert.Equal(2, all.Count);
        Assert.Equal(2, Assert.Single(pending).Id);
    }
}

public class NewsletterTransferCommandTests
{
    [Fact]
    public async Task Status_changes_and_bulk_transfer_marks_reach_the_repository()
    {
        var signups = new RecordingNewsletterSignupRepository();

        await new SetNewsletterTransferStatus.Handler(signups).HandleAsync(new SetNewsletterTransferStatus.Command(5, NewsletterTransferStatus.Transferred));
        await new MarkNewsletterSignupsTransferred.Handler(signups).HandleAsync(new MarkNewsletterSignupsTransferred.Command([1, 2]));

        Assert.Equal((5, NewsletterTransferStatus.Transferred), Assert.Single(signups.StatusChanges));
        Assert.Equal([1, 2], signups.MarkedTransferred);
    }
}
