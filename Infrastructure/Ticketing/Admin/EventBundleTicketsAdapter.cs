using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class EventBundleTicketsAdapter(IScopeProvider scopeProvider) : IEventBundleTickets
{
    public async Task<IReadOnlyList<EventBundleTicket>> GetByBundleAsync(int bundleId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(
            "SELECT t.Uuid, t.EventId, b.Reference " +
            "FROM EventTickets t " +
            "JOIN EventTicketBundles b ON b.Id = t.BundleId " +
            "WHERE t.BundleId = @0 ORDER BY t.Id",
            new object[] { bundleId });
        return rows
            .Select(r => new EventBundleTicket(
                Guid.TryParse(r.Uuid, out var g) ? g : Guid.Empty, r.EventId, r.Reference ?? ""))
            .Where(t => t.Uuid != Guid.Empty)
            .ToList();
    }

    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int EventId { get; set; }
        public string Reference { get; set; } = "";
    }
}

public sealed class EventBundleTicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventBundleTickets, EventBundleTicketsAdapter>();
}
