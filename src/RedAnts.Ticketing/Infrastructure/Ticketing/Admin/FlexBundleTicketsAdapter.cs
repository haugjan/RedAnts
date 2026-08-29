using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
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
            "SELECT t.Uuid, t.SeasonId, b.Reference, t.Category, " +
            "t.BuyerType, t.BuyerFirstName, t.BuyerLastName, t.BuyerCompany, t.BuyerEmail, " +
            "t.Salutation, t.Birthday, t.Street, t.AddressLine2, t.PostalCode, t.City, t.Country, t.Phone " +
            "FROM SeasonSingleTickets t " +
            "JOIN FlexTicketBundles b ON b.Id = t.BundleId " +
            "WHERE t.BundleId IN (@0) ORDER BY b.Reference, t.Id",
            new object[] { bundleIds });
        return rows
            .Select(r => new FlexBundleTicket(
                Guid.TryParse(r.Uuid, out var g) ? g : Guid.Empty, r.SeasonId, r.Reference ?? "",
                (TicketCategory)r.Category,
                CardHolder.Create((BuyerType)(r.BuyerType ?? 0), r.Salutation, r.BuyerCompany,
                    r.BuyerFirstName, r.BuyerLastName, r.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null,
                    r.BuyerEmail, r.Street, r.AddressLine2, r.PostalCode, r.City, r.Country, r.Phone)))
            .Where(t => t.Uuid != Guid.Empty)
            .ToList();
    }

    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int SeasonId { get; set; }
        public string Reference { get; set; } = "";
        public int Category { get; set; }
        public int? BuyerType { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public string? BuyerCompany { get; set; }
        public string? BuyerEmail { get; set; }
        public string? Salutation { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Street { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
    }
}

public sealed class FlexBundleTicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IFlexBundleTickets, FlexBundleTicketsAdapter>();
}
