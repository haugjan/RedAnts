using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Purchase;

/// <summary>Finalizes a purchase once payment is confirmed: marks the ticket paid and sends the
/// confirmation e-mail (fire-and-forget; a mail failure never undoes the paid state).</summary>
public sealed class CompletePurchase(
    ISingleTickets singleTickets,
    ISeasonTickets seasonTickets,
    IEvents events,
    ISeasons seasons,
    ITicketEmail email,
    ILogger<CompletePurchase> logger)
{
    public async Task<bool> CompleteSingleAsync(Guid ticketRef)
    {
        var ticket = await singleTickets.FindByTicketGuidAsync(ticketRef);
        if (ticket is null) return false;
        if (ticket.PayStatus == PayStatus.Paid) return true;

        ticket.MarkPaid();
        await singleTickets.UpdateAsync(ticket);

        var evt = await events.FindByIdAsync(ticket.EventId);
        if (evt is not null)
        {
            try { await email.SendSingleTicketConfirmationAsync(ticket, evt); }
            catch (Exception ex) { logger.LogError(ex, "Ticket confirmation email failed for {Ref}; ticket is paid.", ticketRef); }
        }
        return true;
    }

    public async Task<bool> CompleteSeasonAsync(Guid ticketRef)
    {
        var ticket = await seasonTickets.FindByTicketGuidAsync(ticketRef);
        if (ticket is null) return false;
        if (ticket.PayStatus == PayStatus.Paid) return true;

        ticket.MarkPaid();
        await seasonTickets.UpdateAsync(ticket);

        var season = await seasons.FindByIdAsync(ticket.SeasonId);
        if (season is not null)
        {
            try { await email.SendSeasonTicketConfirmationAsync(ticket, season); }
            catch (Exception ex) { logger.LogError(ex, "Season ticket confirmation email failed for {Ref}; ticket is paid.", ticketRef); }
        }
        return true;
    }
}
