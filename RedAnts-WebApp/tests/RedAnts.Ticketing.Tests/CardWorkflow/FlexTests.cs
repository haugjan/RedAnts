using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.CardWorkflow;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.Ports;
using Xunit;

namespace RedAnts.Ticketing.Tests.CardWorkflow;

public class FlexTests
{
    private const int SeasonId = 3;

    private static CardHolder Holder(string? firstName = "Anna", string? lastName = "Muster") =>
        CardHolder.Create(BuyerType.Private, null, null, firstName, lastName, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Create_bundle_validates_reference_quantity_and_uniqueness()
    {
        var bundles = new RecordingFlexBundles();
        bundles.Existing.Add("Sponsor A");
        var handler = new CreateFlexBundle.Handler(bundles);

        var blank = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateFlexBundle.Command(SeasonId, TicketCategory.Adult, "", 2, null, null, null)));
        var tooLong = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateFlexBundle.Command(SeasonId, TicketCategory.Adult, new string('x', 51), 2, null, null, null)));
        var quantity = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateFlexBundle.Command(SeasonId, TicketCategory.Adult, "Neu", 0, null, null, null)));
        var duplicate = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateFlexBundle.Command(SeasonId, TicketCategory.Adult, "SPONSOR A", 2, null, null, null)));

        Assert.Equal("Bitte ein Bundle angeben.", blank.Message);
        Assert.Contains("50 Zeichen", tooLong.Message);
        Assert.Equal("Menge muss mindestens 1 sein.", quantity.Message);
        Assert.Contains("in dieser Saison bereits vergeben", duplicate.Message);
        Assert.Empty(bundles.Created);
    }

    [Fact]
    public async Task Create_bundle_and_empty_bundle_trim_the_reference()
    {
        var bundles = new RecordingFlexBundles();

        await new CreateFlexBundle.Handler(bundles).HandleAsync(new CreateFlexBundle.Command(SeasonId, TicketCategory.Adult, " Team ", 4, "admin", null, 9));
        await new CreateEmptyFlexBundle.Handler(bundles).HandleAsync(new CreateEmptyFlexBundle.Command(SeasonId, TicketCategory.Adult, " Leer ", "admin", null));

        Assert.Equal(2, bundles.Created.Count);
        Assert.Equal(("Team", 4), (bundles.Created[0].Reference, bundles.Created[0].Quantity));
        Assert.Equal(("Leer", 0), (bundles.Created[1].Reference, bundles.Created[1].Quantity));
    }

    [Fact]
    public async Task Add_tickets_rejects_a_quantity_below_one()
    {
        var bundles = new RecordingFlexBundles();

        await Assert.ThrowsAsync<DomainException>(() => new AddFlexTickets.Handler(bundles).HandleAsync(
            new AddFlexTickets.Command(1, TicketCategory.Adult, 0, null, null, null)));
        await new AddFlexTickets.Handler(bundles).HandleAsync(new AddFlexTickets.Command(1, TicketCategory.Adult, 2, null, null, null));

        Assert.Equal("add:1:2", Assert.Single(bundles.Calls));
    }

    [Fact]
    public async Task Rename_rejects_an_unknown_bundle()
    {
        var bundles = new RecordingFlexBundles();

        var ex = await Assert.ThrowsAsync<DomainException>(() => new RenameFlexBundle.Handler(bundles).HandleAsync(new RenameFlexBundle.Command(99, "Neu")));

        Assert.Equal("Bundle wurde nicht gefunden.", ex.Message);
    }

    [Fact]
    public async Task Rename_rejects_a_reference_that_is_already_taken()
    {
        var bundles = new RecordingFlexBundles();
        bundles.Bundles[1] = FlexTicketBundle.FromPersistence(1, SeasonId, TicketCategory.Adult, "Alt", SwissTime.Timestamp);
        bundles.Existing.Add("Neu");

        var ex = await Assert.ThrowsAsync<DomainException>(() => new RenameFlexBundle.Handler(bundles).HandleAsync(new RenameFlexBundle.Command(1, "Neu")));

        Assert.Contains("bereits vergeben", ex.Message);
        Assert.Empty(bundles.Saved);
    }

    [Fact]
    public async Task Rename_to_the_same_reference_saves_nothing()
    {
        var bundles = new RecordingFlexBundles();
        bundles.Bundles[1] = FlexTicketBundle.FromPersistence(1, SeasonId, TicketCategory.Adult, "Alt", SwissTime.Timestamp);

        await new RenameFlexBundle.Handler(bundles).HandleAsync(new RenameFlexBundle.Command(1, " Alt "));

        Assert.Empty(bundles.Saved);
    }

    [Fact]
    public async Task Rename_saves_the_renamed_bundle()
    {
        var bundles = new RecordingFlexBundles();
        bundles.Bundles[1] = FlexTicketBundle.FromPersistence(1, SeasonId, TicketCategory.Adult, "Alt", SwissTime.Timestamp);

        await new RenameFlexBundle.Handler(bundles).HandleAsync(new RenameFlexBundle.Command(1, " Neu "));

        Assert.Equal("Neu", Assert.Single(bundles.Saved).Reference);
    }

    [Fact]
    public async Task Rebook_and_box_office_delegate_by_uuid_or_code()
    {
        var bundles = new RecordingFlexBundles();
        var uuid = Guid.NewGuid();

        await new RebookFlexTicket.Handler(bundles).HandleAsync(new RebookFlexTicket.Command(5, uuid, null, "op"));
        await new RebookFlexTicket.Handler(bundles).HandleAsync(new RebookFlexTicket.Command(5, null, "abcd1234", "op"));
        await new ConvertFlexToBoxOffice.Handler(bundles).HandleAsync(new ConvertFlexToBoxOffice.Command(uuid, null, "op"));
        await new ConvertFlexToBoxOffice.Handler(bundles).HandleAsync(new ConvertFlexToBoxOffice.Command(null, null, "op"));

        Assert.Equal([$"rebook-uuid:5:{uuid}", "rebook-code:5:abcd1234", $"boxoffice-uuid:{uuid}", "boxoffice-code:"], bundles.Calls);
    }

    [Fact]
    public async Task Ticket_edits_delegate_to_the_bundle_port()
    {
        var bundles = new RecordingFlexBundles();
        var uuid = Guid.NewGuid();

        await new SetFlexTicketStatus.Handler(bundles).HandleAsync(new SetFlexTicketStatus.Command(uuid, TicketStatus.Blocked));
        await new SetFlexTicketRedeemed.Handler(bundles).HandleAsync(new SetFlexTicketRedeemed.Command(uuid, true));
        await new SetFlexTicketCategory.Handler(bundles).HandleAsync(new SetFlexTicketCategory.Command(uuid, TicketCategory.Youth));
        await new SetFlexTicketHolder.Handler(bundles).HandleAsync(new SetFlexTicketHolder.Command(uuid, Holder()));
        await new DeleteEmptyFlexBundle.Handler(bundles).HandleAsync(new DeleteEmptyFlexBundle.Command(7));

        Assert.Equal([$"status:{uuid}:Blocked", $"redeemed:{uuid}:True", $"category:{uuid}:Youth", $"holder:{uuid}", "delete-empty:7"], bundles.Calls);
    }

    [Fact]
    public async Task Single_ticket_requires_a_bundle_and_a_named_holder()
    {
        var bundles = new RecordingFlexBundles();
        var handler = new CreateSingleFlexTicket.Handler(bundles);

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateSingleFlexTicket.Command(SeasonId, TicketCategory.Adult, "", Holder(), null, null)));
        var noName = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new CreateSingleFlexTicket.Command(SeasonId, TicketCategory.Adult, "Team", Holder(null, null), null, null)));
        await handler.HandleAsync(new CreateSingleFlexTicket.Command(SeasonId, TicketCategory.Adult, " Team ", Holder(), null, null));

        Assert.Equal("Bitte Vor-/Nachname oder Firmenname angeben.", noName.Message);
        Assert.Equal($"single:{SeasonId}:Team", Assert.Single(bundles.Calls));
    }

    [Fact]
    public async Task Import_deletion_and_mail_delegate_to_their_ports()
    {
        var bundles = new RecordingFlexBundles();
        var deletion = new RecordingTicketDeletion();
        var mailer = new RecordingFlexMailer();
        var uuid = Guid.NewGuid();
        var rows = new List<TicketImportRow> { new(null, null, null, null, Holder()) };

        var imported = await new ImportFlexTickets.Handler(bundles).HandleAsync(new ImportFlexTickets.Command(SeasonId, rows, "Import", TicketCategory.Adult, null, null));
        await new DeleteFlexTicket.Handler(deletion).HandleAsync(new DeleteFlexTicket.Command(uuid));
        var sent = await new SendFlexTicketMail.Handler(mailer).HandleAsync(
            new SendFlexTicketMail.Command(new FlexMailTicket(uuid, "a@b.ch", "Anna", SeasonId, "Erwachsen"), "Betreff", "Text"));

        Assert.Equal((1, 0), imported);
        Assert.Equal($"import:{SeasonId}:Import", Assert.Single(bundles.Calls));
        Assert.Equal($"flex:{uuid}", Assert.Single(deletion.Deleted));
        Assert.True(sent.Success);
        Assert.Equal("Betreff", Assert.Single(mailer.Sent).Subject);
    }
}
