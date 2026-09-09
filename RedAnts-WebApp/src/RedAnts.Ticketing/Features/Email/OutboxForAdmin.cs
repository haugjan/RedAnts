namespace RedAnts.Ticketing.Features.Email;

public sealed record OutboxForAdmin(
    IReadOnlyList<OutboxEntry> Mails,
    int OpenCount,
    int FailedCount,
    int SentCount,
    DateOnly? GraphSecretExpires)
{
    public static readonly OutboxForAdmin Empty = new([], 0, 0, 0, null);
}
