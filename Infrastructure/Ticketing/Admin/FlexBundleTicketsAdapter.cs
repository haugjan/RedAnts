using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Reads a bundle's issued Flextickets straight from <c>SeasonSingleTickets</c> (matched by
/// <c>BundleId</c>). Only the Uuid and SeasonId are needed to build the short code and the ticket token.</summary>
public sealed class FlexBundleTicketsAdapter(IScopeProvider scopeProvider) : IFlexBundleTickets
{
    public async Task<IReadOnlyList<FlexBundleTicket>> GetByBundleAsync(int bundleId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(
            "SELECT Uuid, SeasonId FROM SeasonSingleTickets WHERE BundleId = @0 ORDER BY Id",
            new object[] { bundleId });
        return rows
            .Select(r => new FlexBundleTicket(Guid.TryParse(r.Uuid, out var g) ? g : Guid.Empty, r.SeasonId))
            .Where(t => t.Uuid != Guid.Empty)
            .ToList();
    }

    /// <summary>Projection for the ticket query (mapped by column name).</summary>
    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int SeasonId { get; set; }
    }
}

/// <summary>Registers the Flexticket bundle export reader (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class FlexBundleTicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IFlexBundleTickets, FlexBundleTicketsAdapter>();
}
