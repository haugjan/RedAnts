using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Tickets;

// Ports for QR ticket generation (S3 slice). The scanner side (S5) verifies a scanned code by
// calling ITicketTokens.TryVerify and then records the admission; it does not need to know the
// token's byte layout, only this contract.

/// <summary>The authenticated content of a ticket QR token: which ticket (by its issued
/// <see cref="Uuid"/>), its <see cref="Type"/>, the catalog node it admits to
/// (<see cref="ScopeId"/> = EventId for single-admission tickets, SeasonId for passes/cards) and
/// when it was minted.</summary>
public sealed record TicketTokenData(TicketType Type, Guid Uuid, int ScopeId, DateTimeOffset IssuedAt);

/// <summary>Creates and verifies compact, HMAC-signed ticket tokens. The token is unforgeable
/// without the server secret; it is not encrypted (the scanner must read it).</summary>
public interface ITicketTokens
{
    /// <summary>Mint a signed token string (form <c>RA1.&lt;payload&gt;.&lt;sig&gt;</c>) for an issued ticket.</summary>
    string Create(TicketType type, Guid uuid, int scopeId);

    /// <summary>Verify signature and parse a scanned/received token. Returns false for any token that
    /// is malformed, has the wrong scheme, or fails the signature check.</summary>
    bool TryVerify(string token, out TicketTokenData data);
}

/// <summary>Renders a QR code for a string payload, without any native (System.Drawing) dependency
/// so it runs on the Linux App Service.</summary>
public interface IQrCodeRenderer
{
    /// <summary>QR as an inline SVG string (crisp, scalable) for web pages.</summary>
    string RenderSvg(string content, int pixelsPerModule = 6);

    /// <summary>QR as a <c>data:image/png;base64,…</c> URI for email or portable embedding.</summary>
    string RenderPngDataUri(string content, int pixelsPerModule = 6);
}

/// <summary>A single issued ticket resolved by its Uuid across the ticket tables, with just the
/// fields the public ticket page needs.</summary>
public sealed record IssuedTicket(
    TicketType Type,
    Guid Uuid,
    int ScopeId,
    TicketCategory? Category,
    TicketStatus Status,
    DateTime CreatedAt,
    string? HolderName,
    MemberCategory? MemberCategory = null);

/// <summary>Read-only lookup of an issued ticket by its Uuid. Probes the ticket tables directly
/// (own S3 slice) so it does not depend on the per-type sales repositories.</summary>
public interface IIssuedTicketReader
{
    Task<IssuedTicket?> FindAsync(Guid uuid);
}
