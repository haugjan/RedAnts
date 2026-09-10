namespace RedAnts.Ticketing.Domain.Sales;

public enum NewsletterTransferStatus
{
    Pending,
    Transferred
}

public sealed class NewsletterSignup
{
    public int Id { get; private set; }
    public EmailAddress Email { get; }
    public string? Name { get; }
    public string Source { get; }
    public DateTimeOffset SignedUpAt { get; }
    public NewsletterTransferStatus Status { get; private set; }
    public DateTimeOffset? TransferredAt { get; private set; }

    private NewsletterSignup(int id, EmailAddress email, string? name, string source,
        DateTimeOffset signedUpAt, NewsletterTransferStatus status, DateTimeOffset? transferredAt)
    {
        Id = id;
        Email = email;
        Name = name;
        Source = source;
        SignedUpAt = signedUpAt;
        Status = status;
        TransferredAt = transferredAt;
    }

    public static NewsletterSignup Create(string email, string? name, string source, TimeProvider? time = null)
    {
        var address = EmailAddress.Create(email);

        return new NewsletterSignup(0, address,
            string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            string.IsNullOrWhiteSpace(source) ? "Onlineshop" : source.Trim(),
            SwissTime.TimestampOf(time), NewsletterTransferStatus.Pending, null);
    }

    public static NewsletterSignup FromPersistence(int id, string email, string? name, string source,
        DateTimeOffset signedUpAt, int status, DateTimeOffset? transferredAt) =>
        new(id, EmailAddress.TryCreate(email) ?? default, name, source, signedUpAt, (NewsletterTransferStatus)status, transferredAt);

    public void MarkTransferred(DateTimeOffset at)
    {
        Status = NewsletterTransferStatus.Transferred;
        TransferredAt = at;
    }

    public void MarkPending()
    {
        Status = NewsletterTransferStatus.Pending;
        TransferredAt = null;
    }
}
