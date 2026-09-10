using RedAnts.Ticketing.Features.Tickets.Admin;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class TicketImportCsvTests
{
    private static TicketImportTable Table(string email) => new(
        ["Name", "Vorname", "E-Mail"],
        [new[] { "Muster", "Anna", email }],
        new Dictionary<string, int> { ["lastname"] = 0, ["firstname"] = 1, ["email"] = 2 });

    private static IReadOnlyDictionary<string, int> Map =>
        new Dictionary<string, int> { ["lastname"] = 0, ["firstname"] = 1, ["email"] = 2 };

    [Fact]
    public void A_valid_mail_address_is_imported()
    {
        var result = TicketImportCsv.BuildRows(Table("anna@example.ch"), Map);

        Assert.Empty(result.Warnings);
        Assert.Equal("anna@example.ch", Assert.Single(result.Rows).Holder.Email?.Value);
    }

    [Fact]
    public void A_malformed_mail_address_is_dropped_with_a_warning()
    {
        var result = TicketImportCsv.BuildRows(Table("anna(at)example.ch"), Map);

        Assert.Null(Assert.Single(result.Rows).Holder.Email);
        Assert.Contains("anna(at)example.ch", Assert.Single(result.Warnings));
    }
}
