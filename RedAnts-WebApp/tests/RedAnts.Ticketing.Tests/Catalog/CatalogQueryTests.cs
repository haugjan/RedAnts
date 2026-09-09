using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog;

public class CatalogQueryTests
{
    private const int SeasonId = 3;

    [Fact]
    public async Task Season_choices_come_from_the_reader()
    {
        var reader = new StubSeasonsForAdmin();
        reader.Choices.Add(new SeasonChoice(SeasonId, "Saison 2026/27", new DateOnly(2026, 8, 1), new DateOnly(2027, 5, 31), true));

        var choices = await new GetSeasonChoices.Handler(reader).HandleAsync(new GetSeasonChoices.Query());

        var choice = Assert.Single(choices);
        Assert.Equal(SeasonId, choice.Id);
        Assert.True(choice.IsCurrent);
    }

    [Fact]
    public async Task Seasons_for_admin_pass_the_rows_and_the_create_link_through()
    {
        var reader = new StubSeasonsForAdmin
        {
            Seasons = new SeasonsForAdmin(
                [new SeasonForAdmin(SeasonId, "Saison 2026/27", new DateOnly(2026, 8, 1), new DateOnly(2027, 5, 31), SeasonStatus.Open,
                    4, 400, 120, 12, 34, 5, 60, 2, "/seasons/2026/", "/seasons/2026?secret=abc")],
                "/umbraco/section/content/workspace/document/create/parent/document/1")
        };

        var result = await new GetSeasonsForAdmin.Handler(reader).HandleAsync(new GetSeasonsForAdmin.Query());

        Assert.Same(reader.Seasons, result);
        Assert.Equal("Saison 2026/27", Assert.Single(result.Seasons).Name);
        Assert.NotNull(result.CreateSeasonUrl);
    }

    [Fact]
    public async Task Events_for_admin_are_read_for_the_requested_season()
    {
        var reader = new StubEventsForAdmin();
        reader.BySeason[SeasonId] = new EventsForAdmin(
            [new EventForAdmin(42, "Red Ants vs. Gegner", new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false, EventStatus.Open,
                200, 150, EventAdmissionCounts.Empty, "/event/", "/event?secret=x", true, false, 5m, false)],
            null);

        var result = await new GetEventsForAdmin.Handler(reader).HandleAsync(new GetEventsForAdmin.Query(SeasonId));
        var other = await new GetEventsForAdmin.Handler(reader).HandleAsync(new GetEventsForAdmin.Query(SeasonId + 1));

        var row = Assert.Single(result.Events);
        Assert.Equal(42, row.Id);
        Assert.True(row.SeasonPassRequired);
        Assert.Equal(5m, row.FlexDiscount);
        Assert.Empty(other.Events);
    }

    [Fact]
    public async Task Free_entry_quotas_are_shaped_per_type_with_fixed_counts_only_when_set()
    {
        var freeEntries = new RecordingFreeEntryQuotas();
        freeEntries.Saved[42] = FreeEntryQuota.Create(
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 20 },
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 3, [FreeEntryType.Staff] = 0 });

        var quotas = await new GetEventFreeEntryQuotas.Handler(freeEntries).HandleAsync(new GetEventFreeEntryQuotas.Query(42));

        Assert.Equal(20, quotas.Quotas[FreeEntryType.Player]);
        Assert.Null(quotas.Quotas[FreeEntryType.Staff]);
        Assert.Equal(3, quotas.FixedCounts[FreeEntryType.Player]);
        Assert.Null(quotas.FixedCounts[FreeEntryType.Staff]);
        Assert.Equal(Enum.GetValues<FreeEntryType>().Length, quotas.Quotas.Count);
    }
}
