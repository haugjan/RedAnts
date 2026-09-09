using Microsoft.Extensions.Logging;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CheckoutWorkflow;

public sealed class CapacityReservation(IEventPrices eventPrices, ISeasonPrices seasonPrices, ILogger<CapacityReservation> logger)
{
    private const int Attempts = 2;

    public async Task<CheckResult> TryReserveAsync(OrderSnapshot snapshot)
    {
        var reservedEvents = new List<int>();
        foreach (var eventId in snapshot.EventIds)
        {
            var result = await ReserveEventAsync(eventId, snapshot.EventDemand(eventId));
            if (!result.IsAllowed)
            {
                await ReleaseAsync(snapshot, reservedEvents, []);
                return result;
            }
            reservedEvents.Add(eventId);
        }

        var reservedSeasons = new List<int>();
        foreach (var seasonId in snapshot.SeasonIds)
        {
            var result = await ReserveSeasonAsync(seasonId, snapshot.PassDemand(seasonId));
            if (!result.IsAllowed)
            {
                await ReleaseAsync(snapshot, reservedEvents, reservedSeasons);
                return result;
            }
            reservedSeasons.Add(seasonId);
        }

        return CheckResult.Allow();
    }

    public Task ReleaseAsync(OrderSnapshot snapshot) => ReleaseAsync(snapshot, snapshot.EventIds, snapshot.SeasonIds);

    private async Task<CheckResult> ReserveEventAsync(int eventId, IReadOnlyList<TierDemand> demand)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var price = await eventPrices.GetByEventAsync(eventId);
            if (price is null)
                return demand.FirstOrDefault(d => !d.IsConversion) is { } regular
                    ? CheckResult.Deny(new CapacityDenied.TierUnavailable(regular.TierId))
                    : CheckResult.Allow();

            var result = price.Reserve(demand, await eventPrices.GetUsageAsync(eventId), SwissTime.Today);
            if (!result.IsAllowed) return result;
            try
            {
                await eventPrices.SaveReservationAsync(price);
                return result;
            }
            catch (ConcurrencyException)
            {
            }
        }
        return CheckResult.Deny(new CapacityDenied.Contended());
    }

    private async Task<CheckResult> ReserveSeasonAsync(int seasonId, IReadOnlyList<TierDemand> demand)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var price = await seasonPrices.GetBySeasonAsync(seasonId);
            if (price is null)
                return CheckResult.Deny(new CapacityDenied.TierUnavailable(demand.Count > 0 ? demand[0].TierId : 0));

            var result = price.ReservePasses(demand, await seasonPrices.GetPassUsageAsync(seasonId), SwissTime.Today);
            if (!result.IsAllowed) return result;
            try
            {
                await seasonPrices.SaveReservationAsync(price);
                return result;
            }
            catch (ConcurrencyException)
            {
            }
        }
        return CheckResult.Deny(new CapacityDenied.Contended());
    }

    private async Task ReleaseAsync(OrderSnapshot snapshot, IEnumerable<int> eventIds, IEnumerable<int> seasonIds)
    {
        foreach (var eventId in eventIds)
            await ReleaseEventAsync(eventId, snapshot.EventDemand(eventId));
        foreach (var seasonId in seasonIds)
            await ReleaseSeasonAsync(seasonId, snapshot.PassDemand(seasonId));
    }

    private async Task ReleaseEventAsync(int eventId, IReadOnlyList<TierDemand> demand)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            try
            {
                var price = await eventPrices.GetByEventAsync(eventId);
                if (price is null) return;
                price.Release(demand);
                await eventPrices.SaveReservationAsync(price);
                return;
            }
            catch (ConcurrencyException)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Reservation for event {EventId} could not be released.", eventId);
                return;
            }
        }
        logger.LogWarning("Reservation for event {EventId} could not be released after {Attempts} attempts.", eventId, Attempts);
    }

    private async Task ReleaseSeasonAsync(int seasonId, IReadOnlyList<TierDemand> demand)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            try
            {
                var price = await seasonPrices.GetBySeasonAsync(seasonId);
                if (price is null) return;
                price.ReleasePasses(demand);
                await seasonPrices.SaveReservationAsync(price);
                return;
            }
            catch (ConcurrencyException)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Reservation for season {SeasonId} could not be released.", seasonId);
                return;
            }
        }
        logger.LogWarning("Reservation for season {SeasonId} could not be released after {Attempts} attempts.", seasonId, Attempts);
    }
}
