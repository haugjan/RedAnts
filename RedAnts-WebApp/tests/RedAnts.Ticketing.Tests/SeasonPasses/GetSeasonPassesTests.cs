using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.SeasonPasses;
using Xunit;

namespace RedAnts.Ticketing.Tests.SeasonPasses;

public class GetSeasonPassesTests
{
    private static SeasonPassRow Row(string buyer, string? reference, TicketStatus status = TicketStatus.Valid, string? email = null) =>
        new(Guid.NewGuid(), "Erwachsen", 300m, status, SwissTime.Timestamp, 0, buyer, null, null, "https://t/x",
            Reference: reference, Email: email);

    private static FakeSeasonPassListReader Reader()
    {
        var reader = new FakeSeasonPassListReader();
        reader.Rows.Add(Row("Anna Muster", "Sponsor B", email: "anna@example.ch"));
        reader.Rows.Add(Row("Ben Beispiel", "Sponsor A"));
        reader.Rows.Add(Row("Carla Storno", "Sponsor A", TicketStatus.Cancelled));
        reader.Rows.Add(Row("Dora Ohne", null));
        return reader;
    }

    [Fact]
    public async Task Returns_all_passes_of_the_season_with_the_sorted_bundles_and_the_total()
    {
        var reader = Reader();

        var result = await new GetSeasonPasses.Handler(reader).HandleAsync(new GetSeasonPasses.Query(3));

        Assert.Equal(3, Assert.Single(reader.Requested));
        Assert.Equal(4, result.Passes.Count);
        Assert.Equal(4, result.Total);
        Assert.Equal(["Sponsor A", "Sponsor B"], result.Bundles);
    }

    [Fact]
    public async Task Filters_by_bundle_and_keeps_the_total_of_the_season()
    {
        var result = await new GetSeasonPasses.Handler(Reader()).HandleAsync(new GetSeasonPasses.Query(3, "Sponsor A"));

        Assert.Equal(["Ben Beispiel", "Carla Storno"], result.Passes.Select(p => p.BuyerName));
        Assert.Equal(4, result.Total);
    }

    [Fact]
    public async Task Searches_every_term_across_name_mail_bundle_and_status()
    {
        var handler = new GetSeasonPasses.Handler(Reader());

        var byMail = await handler.HandleAsync(new GetSeasonPasses.Query(3, Search: "anna@"));
        var byStatus = await handler.HandleAsync(new GetSeasonPasses.Query(3, Search: "storniert sponsor"));
        var nothing = await handler.HandleAsync(new GetSeasonPasses.Query(3, Search: "anna beispiel"));

        Assert.Equal("Anna Muster", Assert.Single(byMail.Passes).BuyerName);
        Assert.Equal("Carla Storno", Assert.Single(byStatus.Passes).BuyerName);
        Assert.Empty(nothing.Passes);
    }

    [Fact]
    public async Task Export_returns_all_passes_or_only_the_selected_bundles()
    {
        var handler = new GetSeasonPassesForExport.Handler(Reader());

        var all = await handler.HandleAsync(new GetSeasonPassesForExport.Query(3, []));
        var selected = await handler.HandleAsync(new GetSeasonPassesForExport.Query(3, ["Sponsor B"]));

        Assert.Equal(4, all.Count);
        Assert.Equal("Anna Muster", Assert.Single(selected).BuyerName);
    }

    [Fact]
    public async Task Mail_template_comes_from_the_mailer_defaults()
    {
        var template = await new GetSeasonPassMailTemplate.Handler(new RecordingSeasonPassMailer()).HandleAsync(new GetSeasonPassMailTemplate.Query());

        Assert.Equal(("Betreff", "Text"), (template.Subject, template.Body));
    }

    [Fact]
    public async Task SendSeasonPassMail_reports_an_unknown_pass_instead_of_sending()
    {
        var mailer = new RecordingSeasonPassMailer();

        var result = await new SendSeasonPassMail.Handler(new InMemorySeasonPasses(), mailer).HandleAsync(
            new SendSeasonPassMail.Command(Guid.NewGuid(), "Erwachsen", null, "Betreff", "Text"));

        Assert.False(result.Success);
        Assert.Empty(mailer.Sent);
    }
}
