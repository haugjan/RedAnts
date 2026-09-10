using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record RelatedTicket(
    string Token,
    string Kicker,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? DisplayName,
    string TypeKey);

public static class GetMyTickets
{
    public sealed record Query(Guid Uuid);

    public sealed class Handler(IMyTicketsReader myTickets, IIssuedTicketReader tickets, ITicketTokens tokens, WebTicketResolution resolution)
    {
        public async Task<IReadOnlyList<RelatedTicket>> HandleAsync(Query query)
        {
            var emails = await myTickets.FindIdentityEmailsAsync(query.Uuid);
            if (emails.Count == 0) return [];

            var summaries = (await myTickets.GetRelatedAsync(emails))
                .Where(s => s.Uuid != query.Uuid && s.Status == TicketStatus.Valid)
                .DistinctBy(s => s.Uuid)
                .ToList();

            var result = new List<RelatedTicket>();
            foreach (var s in summaries)
            {
                if (result.Count >= 30) break;
                var context = await resolution.ContextAsync(s.Type, s.ScopeId);
                if (!context.ScopeIsCurrent) continue;
                var issued = await tickets.FindAsync(s.Uuid);
                result.Add(new RelatedTicket(
                    Token: tokens.CreateShort(s.Uuid),
                    Kicker: TicketDisplay.Kicker(s.Type),
                    TypeLabel: TicketDisplay.TypeLabel(s.Type),
                    ScopeName: context.ScopeName,
                    DateText: s.Type == TicketType.EventTicket ? context.DateText : null,
                    DisplayName: WebTicketResolution.FirstNonEmpty(issued?.CustomName, issued?.HolderName ?? issued?.BuyerName),
                    TypeKey: WebTicketResolution.TypeKey(s.Type, issued?.MemberCategory)));
            }
            return result;
        }
    }
}
