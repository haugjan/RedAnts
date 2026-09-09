using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Admission.Infrastructure;

public sealed class AdmissionFactsReader(
    IScopeProvider scopeProvider,
    IIssuedTicketReader tickets,
    IEvents events,
    IEventConversionRules conversionRules) : IAdmissionFactsReader
{
    public async Task<AdmissionFacts> ReadAsync(int eventId, TicketType ticketType, Guid ticketUuid)
    {
        var issued = await tickets.FindAsync(ticketUuid);
        if (issued is not { Status: TicketStatus.Valid })
            return new AdmissionFacts(issued, null, null, false, false, null, null);

        int? eventSeasonId = null;
        var requiresConversion = false;
        if (ticketType != TicketType.EventTicket)
        {
            eventSeasonId = (await events.FindByIdAsync(eventId))?.SeasonId;
            requiresConversion = (await conversionRules.GetByEventAsync(eventId)).Any(r => r.CardType == ticketType);
        }

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var key = ticketUuid.ToString();

        int? redeemedEventId = null;
        var isBoxOfficeFlex = false;
        if (ticketType == TicketType.SeasonSingle)
        {
            redeemedEventId = await db.ExecuteScalarAsync<int?>("SELECT RedeemedEventId FROM SeasonSingleTickets WHERE Uuid = @0", key);
            isBoxOfficeFlex = await db.ExecuteScalarAsync<bool>("SELECT BoxOffice FROM SeasonSingleTickets WHERE Uuid = @0", key);
        }

        int? originType = null;
        string? originCardUuid = null;
        if (ticketType == TicketType.EventTicket)
        {
            var origin = await db.FirstOrDefaultAsync<EventTicketRecord>("WHERE Uuid = @0", key);
            originType = origin?.OriginType;
            originCardUuid = origin?.OriginCardUuid;
        }

        return new AdmissionFacts(issued, eventSeasonId, redeemedEventId, isBoxOfficeFlex, requiresConversion, originType, originCardUuid);
    }
}
