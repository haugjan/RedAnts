using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.FlexTickets.Infrastructure;
using Xunit;

namespace RedAnts.Ticketing.Tests.Database;

[Collection(AgentDatabaseCollection.Name)]
public class FlexTicketBundleRepositoryDatabaseTests : IAsyncLifetime
{
    private readonly AgentDatabaseFixture _agent = new();

    public Task InitializeAsync() => _agent.InitializeAsync();

    public Task DisposeAsync() => _agent.DisposeAsync();

    private FlexTicketBundleRepository Bundles => new(_agent.Scopes);

    private Task<int> TicketCountAsync(int bundleId) => _agent.Database.ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM SeasonSingleTickets WHERE BundleId = @0", bundleId);

    [DatabaseFact]
    public async Task A_bundle_is_created_with_its_tickets_and_read_back()
    {
        var seasonId = _agent.UnusedId;
        var bundleId = await Bundles.CreateAsync(seasonId, TicketCategory.Youth, " Sponsoren ", 3, "Tester", "tester@example.ch");

        var bundle = await Bundles.GetByIdAsync(bundleId);

        Assert.NotNull(bundle);
        Assert.Equal(seasonId, bundle.SeasonId);
        Assert.Equal("Sponsoren", bundle.Reference);
        Assert.Equal(TicketCategory.Youth, bundle.Category);
        Assert.Equal("Tester", bundle.CreatedByName);
        Assert.Equal(3, await TicketCountAsync(bundleId));
        Assert.True(await Bundles.ReferenceExistsAsync(seasonId, "Sponsoren"));
        Assert.False(await Bundles.ReferenceExistsAsync(seasonId, "Gaeste"));

        Assert.Equal(bundleId, await Bundles.AddTicketsAsync(bundleId, TicketCategory.Adult, 2));
        Assert.Equal(5, await TicketCountAsync(bundleId));
    }

    [DatabaseFact]
    public async Task A_bundle_is_renamed_and_only_deleted_while_it_is_empty()
    {
        var seasonId = _agent.UnusedId;
        var filled = await Bundles.CreateAsync(seasonId, TicketCategory.Adult, "Voll", 1);
        var empty = await Bundles.CreateEmptyAsync(seasonId, TicketCategory.Adult, "Leer");

        var bundle = await Bundles.GetByIdAsync(filled);
        bundle!.Rename("Umbenannt");
        await Bundles.SaveAsync(bundle);

        Assert.Equal("Umbenannt", (await Bundles.GetByIdAsync(filled))!.Reference);
        Assert.False(await Bundles.DeleteEmptyAsync(filled));
        Assert.True(await Bundles.DeleteEmptyAsync(empty));
        Assert.Null(await Bundles.GetByIdAsync(empty));
    }

    [DatabaseFact]
    public async Task A_ticket_moves_to_another_bundle_of_the_same_season()
    {
        var seasonId = _agent.UnusedId;
        var source = await Bundles.CreateAsync(seasonId, TicketCategory.Adult, "Quelle", 1);
        var target = await Bundles.CreateEmptyAsync(seasonId, TicketCategory.Adult, "Ziel");
        var uuid = await _agent.Database.ExecuteScalarAsync<string>(
            "SELECT TOP 1 Uuid FROM SeasonSingleTickets WHERE BundleId = @0", source);

        var moved = await Bundles.RebookByCodeAsync(target, uuid[..8], "Tester");

        Assert.Equal(FlexRebookStatus.Moved, moved.Status);
        Assert.Equal("Quelle", moved.FromBundle);
        Assert.Equal("Ziel", moved.ToBundle);
        Assert.Equal(0, await TicketCountAsync(source));
        Assert.Equal(1, await TicketCountAsync(target));

        Assert.Equal(FlexRebookStatus.AlreadyInTarget, (await Bundles.RebookByCodeAsync(target, uuid[..8], "Tester")).Status);
        Assert.Equal(FlexRebookStatus.NotFound, (await Bundles.RebookByCodeAsync(target, "ffffffff", "Tester")).Status);
    }

    [DatabaseFact]
    public async Task A_ticket_converted_to_the_box_office_lands_in_the_box_office_bundle()
    {
        var seasonId = _agent.UnusedId;
        var bundleId = await Bundles.CreateAsync(seasonId, TicketCategory.Adult, "Vorverkauf", 1);
        var uuid = await _agent.Database.ExecuteScalarAsync<string>(
            "SELECT TOP 1 Uuid FROM SeasonSingleTickets WHERE BundleId = @0", bundleId);

        var converted = await Bundles.ConvertToBoxOfficeByUuidAsync(Guid.Parse(uuid), "Tester");

        Assert.Equal(FlexBoxOfficeStatus.Converted, converted.Status);
        Assert.Equal(FlexBoxOfficeStatus.AlreadyBoxOffice, (await Bundles.ConvertToBoxOfficeByUuidAsync(Guid.Parse(uuid), "Tester")).Status);

        var boxOffice = await _agent.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets t INNER JOIN FlexTicketBundles b ON b.Id = t.BundleId " +
            "WHERE t.Uuid = @0 AND t.BoxOffice = 1 AND b.Reference = 'Abendkasse'", uuid);

        Assert.Equal(1, boxOffice);
    }
}
