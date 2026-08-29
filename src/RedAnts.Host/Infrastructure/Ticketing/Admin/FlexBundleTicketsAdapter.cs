using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class FlexBundleTicketsAdapter(IScopeProvider scopeProvider) : IFlexBundleTickets
{
    public async Task<IReadOnlyList<FlexBundleTicket>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds)
    {
        if (bundleIds.Count == 0) return [];
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(
            "SELECT t.Uuid, t.SeasonId, b.Reference " +
            "FROM SeasonSingleTickets t " +
            "JOIN FlexTicketBundles b ON b.Id = t.BundleId " +
            "WHERE t.BundleId IN (@0) ORDER BY b.Reference, t.Id",
            new object[] { bundleIds });
        return rows
            .Select(r => new FlexBundleTicket(
                Guid.TryParse(r.Uuid, out var g) ? g : Guid.Empty, r.SeasonId, r.Reference ?? ""))
            .Where(t => t.Uuid != Guid.Empty)
            .ToList();
    }

    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int SeasonId { get; set; }
        public string Reference { get; set; } = "";
    }
}

public sealed class FlexBundleTicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IFlexBundleTickets, FlexBundleTicketsAdapter>();
}
