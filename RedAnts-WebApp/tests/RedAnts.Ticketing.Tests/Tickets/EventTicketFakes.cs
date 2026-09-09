using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.Tickets;

internal sealed class InMemoryEventTickets : IEventTickets
{
    public List<EventTicket> Stored { get; } = [];
    public List<(Guid Uuid, CardHolder Holder)> Holders { get; } = [];

    public Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventTicket>>(Stored.Where(t => t.EventId == eventId).ToList());

    public Task<IReadOnlyList<EventTicket>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<EventTicket>>(Stored.Where(t => t.OrderId == orderId).ToList());

    public Task<EventTicket> SaveAsync(EventTicket ticket)
    {
        Stored.RemoveAll(t => t.Uuid == ticket.Uuid);
        Stored.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        Holders.Add((uuid, holder));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingTicketDeletion : IAdminTicketDeletion
{
    public List<string> Deleted { get; } = [];

    public Task DeleteEventTicketAsync(Guid uuid) => Record("event", uuid);
    public Task DeleteFlexTicketAsync(Guid uuid) => Record("flex", uuid);
    public Task DeleteSeasonPassAsync(Guid uuid) => Record("pass", uuid);
    public Task DeleteMemberCardAsync(Guid uuid) => Record("member", uuid);
    public Task DeleteFreeEntryAsync(Guid uuid) => Record("free", uuid);

    private Task Record(string kind, Guid uuid)
    {
        Deleted.Add($"{kind}:{uuid}");
        return Task.CompletedTask;
    }
}
