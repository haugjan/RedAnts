using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Tests.CatalogWorkflow;

internal sealed class RecordingEventPrices : IEventPrices
{
    private readonly Dictionary<int, EventPrice> _prices = new();

    public List<EventPrice> Saved { get; } = [];

    public void Seed(EventPrice price) => _prices[price.EventId] = price;

    public Task<EventPrice?> GetByEventAsync(int eventId) =>
        Task.FromResult(_prices.TryGetValue(eventId, out var price) ? price : null);

    public Task<EventPrice> SaveAsync(EventPrice price)
    {
        Saved.Add(price);
        _prices[price.EventId] = price;
        return Task.FromResult(price);
    }

    public Task DeleteAsync(int eventPriceId) => throw new NotSupportedException();

    public Task<CapacityUsage> GetUsageAsync(int eventId) => Task.FromResult(CapacityUsage.None);

    public Task SaveReservationAsync(EventPrice price) => throw new NotSupportedException();
}

internal sealed class RecordingConversionRules : IEventConversionRules
{
    public List<(int EventId, TicketType CardType, decimal? Discount)> Rules { get; } = [];
    public Dictionary<int, bool> ConversionOnly { get; } = new();

    public Task<IReadOnlyList<EventConversionRule>> GetAllAsync() => throw new NotSupportedException();

    public Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId) => throw new NotSupportedException();

    public Task SetAsync(int eventId, TicketType cardType, decimal? discount)
    {
        Rules.Add((eventId, cardType, discount));
        return Task.CompletedTask;
    }

    public Task<bool> GetConversionOnlyAsync(int eventId) => Task.FromResult(ConversionOnly.GetValueOrDefault(eventId));

    public Task SetConversionOnlyAsync(int eventId, bool value)
    {
        ConversionOnly[eventId] = value;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingFreeEntryQuotas : IFreeEntryRepository
{
    public Dictionary<int, FreeEntryQuota> Saved { get; } = new();

    public Task<FreeEntryQuota> GetQuotaAsync(int eventId) =>
        Task.FromResult(Saved.TryGetValue(eventId, out var quota) ? quota : FreeEntryQuota.Unlimited);

    public Task SaveQuotaAsync(int eventId, FreeEntryQuota quota)
    {
        Saved[eventId] = quota;
        return Task.CompletedTask;
    }

    public Task<int> CountGrantedAsync(int eventId, FreeEntryType type) => throw new NotSupportedException();

    public Task<FreeEntry?> FindLatestInsideAsync(int eventId, FreeEntryType type) => throw new NotSupportedException();

    public Task SaveAsync(FreeEntry entry) => throw new NotSupportedException();
}

internal sealed class RecordingEventStatus : IEventStatusPublisher
{
    public List<(int EventId, EventStatus Status)> Changes { get; } = [];

    public Task SetStatusAsync(int eventId, EventStatus status)
    {
        Changes.Add((eventId, status));
        return Task.CompletedTask;
    }
}
