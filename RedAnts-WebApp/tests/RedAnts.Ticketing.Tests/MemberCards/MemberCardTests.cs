using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.MemberCards;

public class MemberCardTests
{
    private static MemberCard StoredCard(InMemoryMemberCards cards)
    {
        var card = MemberCard.FromPersistence(7, Guid.NewGuid(), 3, MemberCategory.RedAnts, null, TicketStatus.Valid,
            SwissTime.Timestamp, "Anna", "Muster", new DateOnly(1990, 5, 1), "anna@example.ch", "REF-1");
        cards.Stored.Add(card);
        return card;
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
        Assert.Equal("neu@example.ch", saved.Email?.Value);
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
        var imported = await new ImportMemberCards.Handler(cards, new RecordingUnitOfWork()).HandleAsync(new ImportMemberCards.Command(3, "IMPORT", MemberCategory.Block4,
            [new MemberImportRow("Muster", "Anna", null), new MemberImportRow("Beispiel", "Ben", null)], "Admin", "admin@redants.ch"));

        var created = Assert.Single(cards.Added);
        Assert.Equal((3, MemberCategory.RedAnts, "REF-9", 2, "Anna"), (created.SeasonId, created.Category, created.Reference, created.Admissions, created.FirstName));
        Assert.Equal(2, imported);
        Assert.Equal((3, "IMPORT", MemberCategory.Block4, 2), Assert.Single(cards.Imports));
    }

    [Fact]
    public async Task CreateMemberCard_rejects_a_malformed_mail_address()
    {
        var cards = new InMemoryMemberCards();

        var error = await Assert.ThrowsAsync<ValidationException>(() => new CreateMemberCard.Handler(cards).HandleAsync(
            new CreateMemberCard.Command(3, MemberCategory.RedAnts, "Anna", "Muster", null, "REF-9",
                "anna(at)example.ch", null, 1, "Admin", "admin@redants.ch")));

        Assert.Equal("email", error.Field);
        Assert.Equal("E-Mail ist keine gültige Adresse.", error.Message);
        Assert.Empty(cards.Added);
    }

    [Fact]
    public async Task CreateMemberCard_rejects_a_birthday_in_the_future()
    {
        var cards = new InMemoryMemberCards();

        var error = await Assert.ThrowsAsync<ValidationException>(() => new CreateMemberCard.Handler(cards).HandleAsync(
            new CreateMemberCard.Command(3, MemberCategory.RedAnts, "Anna", "Muster", SwissTime.Today.AddDays(1), "REF-9",
                null, null, 1, "Admin", "admin@redants.ch")));

        Assert.Equal("birthday", error.Field);
        Assert.Empty(cards.Added);
    }

    [Fact]
    public async Task CreateMemberCard_accepts_a_valid_mail_address()
    {
        var cards = new InMemoryMemberCards();

        await new CreateMemberCard.Handler(cards).HandleAsync(
            new CreateMemberCard.Command(3, MemberCategory.RedAnts, "Anna", "Muster", new DateOnly(1990, 5, 1), "REF-9",
                " anna+saison@example.ch ", null, 1, "Admin", "admin@redants.ch"));

        Assert.Equal("anna+saison@example.ch", Assert.Single(cards.Added).Email?.Value);
    }

    [Fact]
    public async Task DeleteMemberCard_and_SendMemberCardMail_delegate()
    {
        var deletion = new RecordingDeletion();
        var mailer = new RecordingMemberCardMailer();
        var cards = new InMemoryMemberCards();
        var card = StoredCard(cards);

        await new DeleteMemberCard.Handler(deletion).HandleAsync(new DeleteMemberCard.Command(card.Uuid));
        var result = await new SendMemberCardMail.Handler(cards, mailer).HandleAsync(new SendMemberCardMail.Command(card.Uuid, "Betreff", "Text"));

        Assert.Equal(("member", card.Uuid), Assert.Single(deletion.Deleted));
        Assert.True(result.Success);
        Assert.Equal("Betreff", Assert.Single(mailer.Sent).Subject);
        Assert.Same(card, mailer.Sent[0].Card);
    }

    [Fact]
    public async Task The_member_card_import_runs_in_one_unit_of_work()
    {
        var cards = new InMemoryMemberCards();
        var unitOfWork = new RecordingUnitOfWork();

        await new ImportMemberCards.Handler(cards, unitOfWork).HandleAsync(ImportCommand());

        Assert.Equal(1, unitOfWork.Committed);
        Assert.Single(cards.Imports);
    }

    [Fact]
    public async Task A_failing_member_card_import_rolls_the_unit_of_work_back()
    {
        var cards = new InMemoryMemberCards { ImportThrows = true };
        var unitOfWork = new RecordingUnitOfWork();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ImportMemberCards.Handler(cards, unitOfWork).HandleAsync(ImportCommand()));

        Assert.Equal(1, unitOfWork.RolledBack);
        Assert.Equal(0, unitOfWork.Committed);
        Assert.Empty(cards.Imports);
    }

    private static ImportMemberCards.Command ImportCommand() =>
        new(3, "IMPORT", MemberCategory.Block4, [new MemberImportRow("Muster", "Anna", null)], "Admin", "admin@redants.ch");
}
