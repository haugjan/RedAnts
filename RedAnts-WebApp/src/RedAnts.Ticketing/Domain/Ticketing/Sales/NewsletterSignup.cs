namespace RedAnts.Domain.Ticketing.Sales;

public enum NewsletterTransferStatus
{
    Pending,
    Transferred
}

public sealed class NewsletterSignup
{
    public int Id { get; private set; }
    public string Email { get; }
    public string? Name { get; }
    public string Source { get; }
    public DateTimeOffset SignedUpAt { get; }
    public NewsletterTransferStatus Status { get; private set; }
    public DateTimeOffset? TransferredAt { get; private set; }

    private NewsletterSignup(int id, string email, string? name, string source,
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

    public static NewsletterSignup Create(string email, string? name, string source)
    {
        var address = (email ?? "").Trim();
        if (address.Length < 5 || !address.Contains('@') || !address.Contains('.'))
            throw new DomainException("Für die Newsletter-Anmeldung ist eine gültige E-Mail-Adresse nötig.");

        return new NewsletterSignup(0, address,
            string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            string.IsNullOrWhiteSpace(source) ? "Onlineshop" : source.Trim(),
            SwissTime.Timestamp, NewsletterTransferStatus.Pending, null);
    }

    public static NewsletterSignup FromPersistence(int id, string email, string? name, string source,
        DateTimeOffset signedUpAt, int status, DateTimeOffset? transferredAt) =>
        new(id, email, name, source, signedUpAt, (NewsletterTransferStatus)status, transferredAt);

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
