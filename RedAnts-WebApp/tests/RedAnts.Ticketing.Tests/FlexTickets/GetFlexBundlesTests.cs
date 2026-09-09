using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.FlexTickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.FlexTickets;

public class GetFlexBundlesTests
{
    private const int SeasonId = 3;

    private static FlexTicketRow Ticket(string first, string last, TicketStatus status = TicketStatus.Valid, string? email = null) =>
        new(Guid.NewGuid(), SeasonId, status, false, null, SwissTime.Timestamp, "https://t/x",
            Holder: CardHolder.Create(BuyerType.Private, null, null, first, last, null, email, null, null, null, null, null, null));

    private static FakeFlexBundleTicketsReader TicketsReader()
    {
        var reader = new FakeFlexBundleTicketsReader();
        reader.ByBundle[1] = [Ticket("Anna", "Muster", email: "anna@example.ch"), Ticket("Ben", "Beispiel", TicketStatus.Blocked)];
        reader.ByBundle[2] = [Ticket("Carla", "Dritte")];
        return reader;
    }

    [Fact]
    public async Task Bundles_come_from_the_list_reader_for_the_season()
    {
        var reader = new FakeFlexBundleListReader();
        reader.Rows.Add(new FlexBundleRow(1, SeasonId, TicketCategory.Adult, "Sponsor", SwissTime.Timestamp, 2, 0));
        reader.Rows.Add(new FlexBundleRow(2, 9, TicketCategory.Adult, "Andere Saison", SwissTime.Timestamp, 1, 0));

        var bundles = await new GetFlexBundles.Handler(reader).HandleAsync(new GetFlexBundles.Query(SeasonId));

        Assert.Equal(SeasonId, Assert.Single(reader.Requested));
        Assert.Equal("Sponsor", Assert.Single(bundles).Reference);
    }

    [Fact]
    public async Task Tickets_of_one_bundle_or_of_the_whole_season()
    {
        var reader = TicketsReader();
        var handler = new GetFlexBundleTickets.Handler(reader);

        var bundle = await handler.HandleAsync(new GetFlexBundleTickets.Query(SeasonId, 1));
        var season = await handler.HandleAsync(new GetFlexBundleTickets.Query(SeasonId));

        Assert.Equal(2, bundle.Tickets.Count);
        Assert.Equal(2, bundle.Total);
        Assert.Equal(3, season.Tickets.Count);
        Assert.Equal(["bundle:1", $"season:{SeasonId}"], reader.Calls);
    }

    [Fact]
    public async Task Search_filters_the_tickets_and_keeps_the_total()
    {
        var handler = new GetFlexBundleTickets.Handler(TicketsReader());

        var byMail = await handler.HandleAsync(new GetFlexBundleTickets.Query(SeasonId, 1, "anna@"));
        var byStatus = await handler.HandleAsync(new GetFlexBundleTickets.Query(SeasonId, null, "gesperrt"));

        Assert.Equal("Anna", Assert.Single(byMail.Tickets).Holder!.FirstName);
        Assert.Equal(2, byMail.Total);
        Assert.Equal("Ben", Assert.Single(byStatus.Tickets).Holder!.FirstName);
        Assert.Equal(3, byStatus.Total);
    }

    [Fact]
    public async Task Export_returns_the_tickets_of_the_selected_bundles()
    {
        var reader = TicketsReader();

        var rows = await new GetFlexBundlesForExport.Handler(reader).HandleAsync(new GetFlexBundlesForExport.Query([2, 1]));

        Assert.Equal(3, rows.Count);
        Assert.Equal("bundles:2,1", Assert.Single(reader.Calls));
    }

    [Fact]
    public async Task Mail_template_comes_from_the_mailer_defaults()
    {
        var template = await new GetFlexTicketMailTemplate.Handler(new RecordingFlexMailer()).HandleAsync(new GetFlexTicketMailTemplate.Query());

        Assert.Equal(("Dein Flexticket", "Hallo"), (template.Subject, template.Body));
    }

    [Fact]
    public async Task Bundle_creation_returns_the_bundle_id()
    {
        var bundles = new RecordingFlexBundles();

        var created = await new CreateFlexBundle.Handler(bundles).HandleAsync(new CreateFlexBundle.Command(SeasonId, TicketCategory.Adult, "Team", 2, null, null, null));
        var added = await new AddFlexTickets.Handler(bundles).HandleAsync(new AddFlexTickets.Command(7, TicketCategory.Adult, 2, null, null, null));

        Assert.Equal(1, created);
        Assert.Equal(7, added);
    }
}
