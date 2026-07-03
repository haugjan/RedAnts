using Microsoft.Extensions.Logging;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Seeding;

public sealed class SampleCardsSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SampleCardsSeeder>();
}

/// <summary>
/// Idempotent boot seed for demo Saisonkarten (<c>SeasonPasses</c>) and Mitgliederkarten
/// (<c>MembershipCards</c>), so the season admin lists have data before a checkout exists. For each
/// season that has none yet, seeds a handful across categories. Runs every boot and only fills what is
/// missing. Writes directly via NPoco because no repository/port exists for these two tables yet;
/// references the sales domain/record types read-only.
/// </summary>
public sealed class SampleCardsSeeder(
    IContentService contentService,
    IContentTypeService contentTypeService,
    IScopeProvider scopeProvider,
    ILogger<SampleCardsSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Sample season-pass prices per category (season-long validity → higher than a single ticket).
    private static readonly (TicketCategory Category, decimal Price)[] SamplePasses =
    [
        (TicketCategory.Adult, 250m),
        (TicketCategory.Youth, 150m),
        (TicketCategory.Child, 100m),
    ];

    // Sample member-card holders. Name/birthday are optional (revDSG minimization); one is left blank
    // on purpose to exercise the anonymous case.
    private static readonly (TicketCategory Category, decimal Price, string? First, string? Last, DateOnly? Birthday)[] SampleMembers =
    [
        (TicketCategory.Adult, 120m, "Anna", "Muster", new DateOnly(1990, 5, 14)),
        (TicketCategory.Youth, 60m, "Ben", "Beispiel", new DateOnly(2009, 11, 2)),
        (TicketCategory.Child, 40m, null, null, null),
    ];

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var seasonType = contentTypeService.Get(A.SeasonType);
            if (seasonType is null) return;

            foreach (var season in contentService.GetPagedOfType(seasonType.Id, 0, 1000, out _, null))
            {
                await EnsureSeasonPassesAsync(season.Id, season.Name);
                await EnsureMemberCardsAsync(season.Id, season.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SampleCardsSeeder failed.");
        }
    }

    private async Task EnsureSeasonPassesAsync(int seasonId, string? seasonName)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var existing = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonPasses WHERE SeasonId = @0", seasonId);
        if (existing > 0) return;

        foreach (var s in SamplePasses)
        {
            var pass = SeasonPass.Create(seasonId, s.Category, s.Price, null);
            await scope.Database.InsertAsync(ToRecord(pass));
        }
        logger.LogInformation("SampleCardsSeeder: seeded {Count} season passes for season '{Name}'.",
            SamplePasses.Length, seasonName);
    }

    private async Task EnsureMemberCardsAsync(int seasonId, string? seasonName)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var existing = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MembershipCards WHERE SeasonId = @0", seasonId);
        if (existing > 0) return;

        foreach (var m in SampleMembers)
        {
            var card = MemberCard.Create(seasonId, m.Category, m.Price, null, m.First, m.Last, m.Birthday);
            await scope.Database.InsertAsync(ToRecord(card));
        }
        logger.LogInformation("SampleCardsSeeder: seeded {Count} member cards for season '{Name}'.",
            SampleMembers.Length, seasonName);
    }

    private static SeasonPassRecord ToRecord(SeasonPass p) => new()
    {
        Uuid = p.Uuid.ToString(),
        SeasonId = p.SeasonId,
        Category = (int)p.Category,
        Price = p.Price,
        OrderId = p.OrderId,
        Status = (int)p.Status,
        CreatedAt = p.CreatedAt
    };

    private static MemberCardRecord ToRecord(MemberCard c) => new()
    {
        Uuid = c.Uuid.ToString(),
        SeasonId = c.SeasonId,
        Category = (int)c.Category,
        Price = c.Price,
        OrderId = c.OrderId,
        Status = (int)c.Status,
        CreatedAt = c.CreatedAt,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Birthday = c.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : null
    };
}
