using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Newsletter;

public sealed record NewsletterSignupRow(
    int Id,
    string Email,
    string? Name,
    string Source,
    DateTimeOffset SignedUpAt,
    NewsletterTransferStatus Status,
    DateTimeOffset? TransferredAt);
