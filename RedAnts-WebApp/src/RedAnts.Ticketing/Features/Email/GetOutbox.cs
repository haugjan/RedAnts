using System.Globalization;

namespace RedAnts.Ticketing.Features.Email;

public static class GetOutbox
{
    public sealed record Query(bool IncludeSent);

    public sealed class Handler(IOutboxReader outbox, IConfiguration config)
    {
        public async Task<OutboxForAdmin> HandleAsync(Query query)
        {
            var mails = await outbox.ListAsync(query.IncludeSent, SwissTime.Timestamp.AddDays(-30));
            return new OutboxForAdmin(
                mails,
                mails.Count(m => m.Status is OutboxStatus.Pending or OutboxStatus.Sending),
                mails.Count(m => m.Status == OutboxStatus.Failed),
                mails.Count(m => m.Status == OutboxStatus.Sent),
                GraphSecretExpiry());
        }

        private DateOnly? GraphSecretExpiry() =>
            DateTime.TryParse(config["Graph:ClientSecretExpires"], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expires)
                ? DateOnly.FromDateTime(expires)
                : null;
    }
}
