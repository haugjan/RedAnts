using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.MemberCards;
using Xunit;

namespace RedAnts.Ticketing.Tests.MemberCards;

public class GetMemberCardsTests
{
    private static MemberCardRow Row(string first, string last, string? reference, MemberCategory category = MemberCategory.RedAnts,
        TicketStatus status = TicketStatus.Valid, string? email = null) =>
        new(Guid.NewGuid(), first, last, null, category, status, SwissTime.Timestamp, 0, reference, "https://t/x", email);

    private static FakeMemberCardListReader Reader()
    {
        var reader = new FakeMemberCardListReader();
        reader.Rows.Add(Row("Anna", "Muster", "Block 4", MemberCategory.Block4, email: "anna@example.ch"));
        reader.Rows.Add(Row("Ben", "Beispiel", "Verein"));
        reader.Rows.Add(Row("Carla", "Storno", "Verein", status: TicketStatus.Cancelled));
        reader.Rows.Add(Row("Dora", "Firma", "Verein", MemberCategory.Company, email: "dora@example.ch"));
        reader.Rows.Add(Row("Emil", "Ohne", null));
        return reader;
    }

    [Fact]
    public async Task Returns_the_cards_with_bundles_mail_batches_and_the_total()
    {
        var reader = Reader();

        var result = await new GetMemberCards.Handler(reader).HandleAsync(new GetMemberCards.Query(3));

        Assert.Equal(3, Assert.Single(reader.Requested));
        Assert.Equal(5, result.Cards.Count);
        Assert.Equal(5, result.Total);
        Assert.Equal(["Block 4", "Verein"], result.Bundles);
        Assert.Equal(["Block 4|1", "Verein|0", "Verein|2"], result.MailBatches.Select(b => b.Key));
        Assert.Equal("Ben", Assert.Single(result.MailBatches[1].Cards).FirstName);
    }

    [Fact]
    public async Task Filters_by_bundle_and_search_terms()
    {
        var handler = new GetMemberCards.Handler(Reader());

        var byBundle = await handler.HandleAsync(new GetMemberCards.Query(3, "Verein"));
        var bySearch = await handler.HandleAsync(new GetMemberCards.Query(3, Search: "storniert"));
        var both = await handler.HandleAsync(new GetMemberCards.Query(3, "Verein", "dora@"));

        Assert.Equal(["Ben", "Carla", "Dora"], byBundle.Cards.Select(c => c.FirstName));
        Assert.Equal("Carla", Assert.Single(bySearch.Cards).FirstName);
        Assert.Equal("Dora", Assert.Single(both.Cards).FirstName);
        Assert.Equal(5, both.Total);
    }

    [Fact]
    public async Task Export_returns_only_the_selected_bundles()
    {
        var handler = new GetMemberCardsForExport.Handler(Reader());

        var selected = await handler.HandleAsync(new GetMemberCardsForExport.Query(3, ["Block 4"]));
        var all = await handler.HandleAsync(new GetMemberCardsForExport.Query(3, []));

        Assert.Equal("Anna", Assert.Single(selected).FirstName);
        Assert.Equal(5, all.Count);
    }

    [Fact]
    public async Task Mail_template_depends_on_the_category()
    {
        var template = await new GetMemberCardMailTemplate.Handler(new RecordingMemberCardMailer())
            .HandleAsync(new GetMemberCardMailTemplate.Query(MemberCategory.Block4));

        Assert.Equal(("Betreff Block4", "Text Block4"), (template.Subject, template.Body));
    }

    [Fact]
    public async Task SendMemberCardMail_reports_an_unknown_card_instead_of_sending()
    {
        var mailer = new RecordingMemberCardMailer();

        var result = await new SendMemberCardMail.Handler(new InMemoryMemberCards(), mailer)
            .HandleAsync(new SendMemberCardMail.Command(Guid.NewGuid(), "Betreff", "Text"));

        Assert.False(result.Success);
        Assert.Empty(mailer.Sent);
    }

    [Fact]
    public async Task CreateMemberCard_rejects_a_blank_bundle()
    {
        var cards = new InMemoryMemberCards();

        await Assert.ThrowsAsync<DomainException>(() => new CreateMemberCard.Handler(cards).HandleAsync(
            new CreateMemberCard.Command(3, MemberCategory.RedAnts, "Anna", "Muster", null, " ", null, null, 1, null, null)));

        Assert.Empty(cards.Added);
    }
}
