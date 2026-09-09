using RedAnts.Ticketing.Features.Helpers;
using Xunit;

namespace RedAnts.Ticketing.Tests.Helpers;

public class GetHelpersTests
{
    private static HelperRow Row(int id, int seasonId, string first, string last, bool active = true) =>
        new(id, seasonId, first, last, $"{first}@example.ch", $"code-{id}", true, [], false, active, SwissTime.Timestamp);

    [Fact]
    public async Task Helpers_come_from_the_list_reader_for_the_season()
    {
        var reader = new FakeHelperListReader();
        reader.Rows.Add(Row(1, 3, "Anna", "Muster"));
        reader.Rows.Add(Row(2, 3, "Ben", "Beispiel", active: false));
        reader.Rows.Add(Row(3, 9, "Carla", "Andere"));

        var helpers = await new GetHelpers.Handler(reader).HandleAsync(new GetHelpers.Query(3));

        Assert.Equal(3, Assert.Single(reader.Requested));
        Assert.Equal(["Anna Muster", "Ben Beispiel"], helpers.Select(h => h.FullName));
        Assert.Equal("Anna Muster", Assert.Single(helpers, h => h.Active).FullName);
    }

    [Fact]
    public async Task Scan_report_passes_the_event_ids_to_the_reader()
    {
        var reader = new FakeHelperScanReportReader();
        reader.Rows.Add(new HelperScanRow(10, "Anna Muster", 4, 1));
        reader.Rows.Add(new HelperScanRow(11, "Anna Muster", 2, 0));
        reader.Rows.Add(new HelperScanRow(12, "Ben Beispiel", 1, 0));

        var rows = await new GetHelperScanReport.Handler(reader).HandleAsync(new GetHelperScanReport.Query([10, 11]));

        Assert.Equal([10, 11], Assert.Single(reader.Requested));
        Assert.Equal(2, rows.Count);
        Assert.Equal(7, rows.Sum(r => r.Total));
    }

    [Fact]
    public async Task Invite_template_comes_from_the_mailer_defaults()
    {
        var template = await new GetHelperInviteTemplate.Handler(new RecordingHelperInviteMailer()).HandleAsync(new GetHelperInviteTemplate.Query());

        Assert.Equal(("Betreff", "Text"), (template.Subject, template.Body));
    }

    [Fact]
    public async Task InviteHelper_reports_an_unknown_helper_instead_of_sending()
    {
        var mailer = new RecordingHelperInviteMailer();

        var result = await new InviteHelperByMail.Handler(new RecordingHelpers(), mailer)
            .HandleAsync(new InviteHelperByMail.Command(99, "Einladung", "Text", "https://scan.redants.ch/scan/x"));

        Assert.False(result.Success);
        Assert.Equal(InviteHelperByMail.NotFound, result.Error);
        Assert.Empty(mailer.Sent);
    }
}
