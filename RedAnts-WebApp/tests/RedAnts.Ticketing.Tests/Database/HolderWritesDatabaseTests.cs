using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using RedAnts.Ticketing.Features.MemberCards.Infrastructure;
using RedAnts.Ticketing.Features.SeasonPasses.Infrastructure;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Xunit;

namespace RedAnts.Ticketing.Tests.Database;

[Collection(AgentDatabaseCollection.Name)]
public class HolderWritesDatabaseTests : IAsyncLifetime
{
    private readonly AgentDatabaseFixture _agent = new();

    public Task InitializeAsync() => _agent.InitializeAsync();

    public Task DisposeAsync() => _agent.DisposeAsync();

    private MemberCardRepository Cards => new(_agent.Scopes);

    private SeasonPassRepository Passes => new(_agent.Scopes, new PriceTierRepository(_agent.Scopes));

    private EventTicketRepository Tickets => new(_agent.Scopes);

    private static CardHolder HolderWithMail() =>
        CardHolder.Create(BuyerType.Private, "Anna", "Muster", null, "Frau", new DateOnly(1990, 5, 4),
            "anna@example.ch", "Musterweg 1", null, "8400", "Winterthur", "CH", null);

    [DatabaseFact]
    public async Task A_member_card_with_an_address_survives_a_status_change()
    {
        var seasonId = _agent.UnusedId;
        var card = MemberCard.Create(seasonId, MemberCategory.RedAnts, "Anna", "Muster", null, "anna@example.ch", "Block 4");
        await Cards.AddAsync(card);
        var saved = await Cards.GetByUuidAsync(card.Uuid);
        Assert.NotNull(saved);

        saved!.SetStatus(TicketStatus.Cancelled);
        await Cards.SaveAsync(saved);

        var stored = await Cards.GetByUuidAsync(card.Uuid);
        Assert.NotNull(stored);
        Assert.Equal(TicketStatus.Cancelled, stored!.Status);
        Assert.Equal("anna@example.ch", stored.Email?.Value);
    }

    [DatabaseFact]
    public async Task A_season_pass_keeps_the_holder_address_it_was_given()
    {
        var seasonId = _agent.UnusedId;
        var pass = await Passes.SaveAsync(SeasonPass.Create(seasonId, null, 220m, null));

        await Passes.SetHolderAsync(pass.Uuid, HolderWithMail());

        var stored = await Passes.GetByUuidAsync(pass.Uuid);
        Assert.NotNull(stored);
        Assert.Equal("anna@example.ch", stored!.Email?.Value);
    }

    [DatabaseFact]
    public async Task An_event_ticket_keeps_the_holder_address_it_was_given()
    {
        var eventId = _agent.UnusedId;
        var ticket = await Tickets.SaveAsync(EventTicket.Create(eventId, TicketCategory.Adult, 20m, null, null, "Tester"));

        await Tickets.SetHolderAsync(ticket.Uuid, HolderWithMail());

        var stored = await Tickets.GetByUuidAsync(ticket.Uuid);
        Assert.NotNull(stored);
        Assert.Equal("anna@example.ch", stored!.Holder?.Email?.Value);
    }
}
