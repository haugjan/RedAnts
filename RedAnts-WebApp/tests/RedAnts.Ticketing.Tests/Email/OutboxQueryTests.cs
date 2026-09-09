using Microsoft.Extensions.Configuration;
using RedAnts.Ticketing.Features.Email;
using Xunit;

namespace RedAnts.Ticketing.Tests.Email;

public class GetOutboxTests
{
    private static IConfiguration Config(string? expires) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Graph:ClientSecretExpires"] = expires }).Build();

    [Fact]
    public async Task Lists_the_mails_with_their_counts_and_the_secret_expiry()
    {
        var reader = new StubOutboxReader();
        reader.Entries.Add(StubOutboxReader.Entry(1, OutboxStatus.Pending));
        reader.Entries.Add(StubOutboxReader.Entry(2, OutboxStatus.Sending));
        reader.Entries.Add(StubOutboxReader.Entry(3, OutboxStatus.Failed));
        reader.Entries.Add(StubOutboxReader.Entry(4, OutboxStatus.Sent));
        var handler = new GetOutbox.Handler(reader, Config("2027-03-31"));

        var result = await handler.HandleAsync(new GetOutbox.Query(IncludeSent: true));

        Assert.True(reader.LastCall!.Value.IncludeSent);
        Assert.InRange(reader.LastCall.Value.SentSince, SwissTime.Timestamp.AddDays(-31), SwissTime.Timestamp.AddDays(-29));
        Assert.Equal(4, result.Mails.Count);
        Assert.Equal((2, 1, 1), (result.OpenCount, result.FailedCount, result.SentCount));
        Assert.Equal(new DateOnly(2027, 3, 31), result.GraphSecretExpires);
    }

    [Fact]
    public async Task A_missing_secret_expiry_stays_empty()
    {
        var result = await new GetOutbox.Handler(new StubOutboxReader(), Config(null)).HandleAsync(new GetOutbox.Query(false));

        Assert.Null(result.GraphSecretExpires);
        Assert.Empty(result.Mails);
    }
}

public class RetryOutboxMailTests
{
    [Fact]
    public async Task Requeues_the_mail_and_reports_whether_it_was_requeued()
    {
        var outbox = new RecordingEmailOutbox();
        outbox.Requeueable.Add(7);
        var handler = new RetryOutboxMail.Handler(outbox);

        Assert.True(await handler.HandleAsync(new RetryOutboxMail.Command(7)));
        Assert.False(await handler.HandleAsync(new RetryOutboxMail.Command(8)));
        Assert.Equal([7, 8], outbox.Requeued);
    }
}
