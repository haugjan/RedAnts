using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CardWorkflow;
using RedAnts.Features.Ticketing.Ports;
using Xunit;

namespace RedAnts.Ticketing.Tests.CardWorkflow;

public class MemberAndPassTests
{
    private static MemberCard StoredCard(InMemoryMemberCards cards)
    {
        var card = MemberCard.FromPersistence(7, Guid.NewGuid(), 3, MemberCategory.RedAnts, null, TicketStatus.Valid,
            SwissTime.Timestamp, "Anna", "Muster", new DateOnly(1990, 5, 1), "anna@example.ch", "REF-1");
        cards.Stored.Add(card);
        return card;
    }

    private static SeasonPass StoredPass(InMemorySeasonPasses passes)
    {
        var pass = SeasonPass.FromPersistence(5, Guid.NewGuid(), 3, 2, 300m, null, TicketStatus.Valid, SwissTime.Timestamp);
        passes.Stored.Add(pass);
        return pass;
    }

    [Fact]
    public async Task EditMemberCard_loads_applies_and_saves()
    {
        var cards = new InMemoryMemberCards();
        var card = StoredCard(cards);
        var address = MemberAddress.Create("Frau", null, "Weg 1", null, "8400", "Winterthur", "Schweiz", "079");

        await new EditMemberCard.Handler(cards).HandleAsync(new EditMemberCard.Command(card.Uuid, " Anna ", "Beispiel", new DateOnly(1991, 1, 2),
            MemberCategory.Block4, TicketStatus.Cancelled, "REF-2", "neu@example.ch", address, 3));

        var saved = Assert.Single(cards.Saved);
        Assert.Same(card, saved);
        Assert.Equal("Anna", saved.FirstName);
        Assert.Equal("Beispiel", saved.LastName);
        Assert.Equal(new DateOnly(1991, 1, 2), saved.Birthday);
        Assert.Equal(MemberCategory.Block4, saved.Category);
        Assert.Equal(TicketStatus.Cancelled, saved.Status);
        Assert.Equal("REF-2", saved.Reference);
        Assert.Equal("neu@example.ch", saved.Email);
        Assert.Equal("Winterthur", saved.Address.City);
        Assert.Equal(3, saved.Admissions);
    }

    [Fact]
    public async Task EditMemberCard_rejects_unknown_cards()
    {
        var cards = new InMemoryMemberCards();

        var ex = await Assert.ThrowsAsync<DomainException>(() => new EditMemberCard.Handler(cards).HandleAsync(
            new EditMemberCard.Command(Guid.NewGuid(), null, null, null, MemberCategory.RedAnts, TicketStatus.Valid, null, null, null, 1)));

        Assert.Equal(EditMemberCard.NotFound, ex.Message);
        Assert.Empty(cards.Saved);
    }

    [Fact]
    public async Task SetMemberCardStatus_and_category_save_the_changed_card()
    {
        var cards = new InMemoryMemberCards();
        var card = StoredCard(cards);

        await new SetMemberCardStatus.Handler(cards).HandleAsync(new SetMemberCardStatus.Command(card.Uuid, TicketStatus.Blocked));
        await new SetMemberCardCategory.Handler(cards).HandleAsync(new SetMemberCardCategory.Command(card.Uuid, MemberCategory.Block4));

        Assert.Equal(2, cards.Saved.Count);
        Assert.Equal(TicketStatus.Blocked, card.Status);
        Assert.Equal(MemberCategory.Block4, card.Category);
    }

    [Fact]
    public async Task CreateMemberCard_and_ImportMemberCards_delegate_to_the_repository()
    {
        var cards = new InMemoryMemberCards();

        await new CreateMemberCard.Handler(cards).HandleAsync(new CreateMemberCard.Command(3, MemberCategory.RedAnts, "Anna", "Muster",
            null, "REF-9", null, null, 2, "Admin", "admin@redants.ch"));
        var imported = await new ImportMemberCards.Handler(cards).HandleAsync(new ImportMemberCards.Command(3, "IMPORT", MemberCategory.Block4,
            [new MemberImportRow("Muster", "Anna", null), new MemberImportRow("Beispiel", "Ben", null)], "Admin", "admin@redants.ch"));

        Assert.Equal((3, MemberCategory.RedAnts, "REF-9", 2), Assert.Single(cards.Created));
        Assert.Equal(2, imported);
        Assert.Equal((3, "IMPORT", MemberCategory.Block4, 2), Assert.Single(cards.Imports));
    }

    [Fact]
    public async Task DeleteMemberCard_and_SendMemberCardMail_delegate()
    {
        var deletion = new RecordingDeletion();
        var mailer = new RecordingMemberCardMailer();
        var cards = new InMemoryMemberCards();
        var card = StoredCard(cards);

        await new DeleteMemberCard.Handler(deletion).HandleAsync(new DeleteMemberCard.Command(card.Uuid));
        var result = await new SendMemberCardMail.Handler(mailer).HandleAsync(new SendMemberCardMail.Command(card, "Betreff", "Text"));

        Assert.Equal(("member", card.Uuid), Assert.Single(deletion.Deleted));
        Assert.True(result.Success);
        Assert.Equal("Betreff", Assert.Single(mailer.Sent).Subject);
    }

    [Fact]
    public async Task EditSeasonPass_applies_price_status_tier_buyer_and_email()
    {
        var passes = new InMemorySeasonPasses();
        var pass = StoredPass(passes);
        var buyer = Buyer.Create(BuyerType.Private, "Anna", "Muster", null);

        await new EditSeasonPass.Handler(passes).HandleAsync(new EditSeasonPass.Command(pass.Uuid, 250.505m, TicketStatus.Cancelled, 9, buyer, "anna@example.ch"));

        Assert.Same(pass, Assert.Single(passes.Saved));
        Assert.Equal(250.50m, pass.Price);
        Assert.Equal(TicketStatus.Cancelled, pass.Status);
        Assert.Equal(9, pass.TierId);
        Assert.Same(buyer, pass.Buyer);
        Assert.Equal("anna@example.ch", pass.Email);
    }

    [Fact]
    public async Task EditSeasonPass_keeps_tier_buyer_and_email_when_not_given()
    {
        var passes = new InMemorySeasonPasses();
        var pass = StoredPass(passes);
        pass.SetEmail("alt@example.ch");

        await new EditSeasonPass.Handler(passes).HandleAsync(new EditSeasonPass.Command(pass.Uuid, 100m, TicketStatus.Valid, null, null, null));

        Assert.Equal(2, pass.TierId);
        Assert.Null(pass.Buyer);
        Assert.Equal("alt@example.ch", pass.Email);
    }

    [Fact]
    public async Task EditSeasonPass_rejects_unknown_passes_and_negative_prices()
    {
        var passes = new InMemorySeasonPasses();
        var pass = StoredPass(passes);
        var handler = new EditSeasonPass.Handler(passes);

        var missing = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new EditSeasonPass.Command(Guid.NewGuid(), 1m, TicketStatus.Valid, null, null, null)));
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new EditSeasonPass.Command(pass.Uuid, -1m, TicketStatus.Valid, null, null, null)));

        Assert.Equal(EditSeasonPass.NotFound, missing.Message);
        Assert.Empty(passes.Saved);
    }

    [Fact]
    public async Task Pass_holder_delete_import_and_mail_delegate()
    {
        var passes = new InMemorySeasonPasses();
        var pass = StoredPass(passes);
        var deletion = new RecordingDeletion();
        var mailer = new RecordingSeasonPassMailer();
        var holder = CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, null, null, null, null, null, null, null);

        await new SetSeasonPassHolder.Handler(passes).HandleAsync(new SetSeasonPassHolder.Command(pass.Uuid, holder));
        await new DeleteSeasonPass.Handler(deletion).HandleAsync(new DeleteSeasonPass.Command(pass.Uuid));
        var imported = await new ImportSeasonPasses.Handler(passes).HandleAsync(new ImportSeasonPasses.Command(3,
            [new TicketImportRow(null, null, null, null, holder)], "BUNDLE", 4, "Admin", "admin@redants.ch"));
        var mail = await new SendSeasonPassMail.Handler(mailer).HandleAsync(new SendSeasonPassMail.Command(pass, "Erwachsen", "Betreff", "Text"));

        Assert.Equal((pass.Uuid, holder), Assert.Single(passes.Holders));
        Assert.Equal(("pass", pass.Uuid), Assert.Single(deletion.Deleted));
        Assert.Equal((1, 0), imported);
        Assert.Equal((3, 1, "BUNDLE", 4), Assert.Single(passes.Imports));
        Assert.True(mail.Success);
        Assert.Equal("Erwachsen", Assert.Single(mailer.Sent).CategoryLabel);
    }
}
