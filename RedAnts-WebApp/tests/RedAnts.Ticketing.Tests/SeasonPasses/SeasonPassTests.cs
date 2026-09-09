using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.MemberCards;
using Xunit;

namespace RedAnts.Ticketing.Tests.SeasonPasses;

public class SeasonPassTests
{
    private static SeasonPass StoredPass(InMemorySeasonPasses passes)
    {
        var pass = SeasonPass.FromPersistence(5, Guid.NewGuid(), 3, 2, 300m, null, TicketStatus.Valid, SwissTime.Timestamp);
        passes.Stored.Add(pass);
        return pass;
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
