namespace RedAnts.Features.Ticketing.Tickets;

public sealed record WalletTicketModel(
    Guid Uuid,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string TicketRef,
    string TicketUrl,
    string AccentHex);

public interface IWalletPass
{
    bool Enabled { get; }

    string? SaveUrl(WalletTicketModel model, string origin);
}
