using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Admission;
using RedAnts.Ticketing.Tests.Catalog;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class WebTicketQueryTests
{
    private static readonly Guid Uuid = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const int EventId = 42;

    private readonly StubTicketTokens _tokens = new();
    private readonly StubIssuedTickets _issued = new();
    private readonly StubEvents _events = new();
    private readonly StubSeasons _seasons = new();
    private readonly StubVenues _venues = new();
    private readonly StubContentUrls _urls = new();
    private readonly StubQr _qr = new();

    public WebTicketQueryTests()
    {
        _tokens.Full["full-token"] = new TicketTokenData(TicketType.EventTicket, Uuid, EventId, default);
        _issued.Tickets[Uuid] = new IssuedTicket(TicketType.EventTicket, Uuid, EventId, TicketCategory.Adult, TicketStatus.Valid,
            SwissTime.Timestamp, null, BuyerName: "Anna Muster", CategoryName: "Erwachsen", CustomName: "Mein Ticket");
        _events.Events.Add(Event.FromPersistence(EventId, "Red Ants vs. Gegner", null, 3, new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false, 7,
            EventStatus.Open, null, "/home.png", "/away.png", null));
        _venues.Venues.Add(Venue.FromPersistence(7, "Halle", null, null, null));
    }

    private WebTicketResolution Resolution => new(_tokens, _issued, _events, _seasons, _venues, _urls);

    [Fact]
    public async Task Web_ticket_is_resolved_from_the_full_token_with_its_context_and_qr()
    {
        var ticket = await new GetWebTicket.Handler(Resolution, _qr, new StubPublicBaseUrl()).HandleAsync(new GetWebTicket.Query("full-token"));

        Assert.NotNull(ticket);
        Assert.True(ticket.Found);
        Assert.True(ticket.Valid);
        Assert.Equal("Red Ants vs. Gegner", ticket.ScopeName);
        Assert.Equal("03.10.2026, 18:00 Uhr", ticket.DateText);
        Assert.Equal("Halle", ticket.VenueName);
        Assert.Equal("Erwachsen", ticket.CategoryLabel);
        Assert.Equal("Mein Ticket", ticket.HolderName);
        Assert.Equal("Anna Muster", ticket.HolderDefault);
        Assert.Equal("11111111", ticket.TicketRef);
        Assert.Equal("svg:https://tickets.test/ticket/short-" + Uuid.ToString("N")[..8], ticket.QrSvg);
        Assert.Single(ticket.Upcoming);
    }

    [Fact]
    public async Task Web_ticket_is_null_for_an_unknown_token_and_not_found_for_an_unknown_ticket()
    {
        var handler = new GetWebTicket.Handler(Resolution, _qr, new StubPublicBaseUrl());
        _tokens.Full["orphan"] = new TicketTokenData(TicketType.SeasonPass, Guid.NewGuid(), 3, default);

        Assert.Null(await handler.HandleAsync(new GetWebTicket.Query("bogus")));
        var orphan = await handler.HandleAsync(new GetWebTicket.Query("orphan"));
        Assert.NotNull(orphan);
        Assert.False(orphan.Found);
        Assert.Equal("Saison", orphan.ScopeName);
    }

    [Fact]
    public async Task My_tickets_list_the_other_valid_tickets_of_the_same_buyer()
    {
        var other = Guid.NewGuid();
        _issued.Tickets[other] = new IssuedTicket(TicketType.SeasonPass, other, 3, TicketCategory.Adult, TicketStatus.Valid, SwissTime.Timestamp, "Bea Muster");
        _seasons.Seasons.Add(Season.FromPersistence(3, "Saison 2026/27", new DateOnly(2026, 8, 1), new DateOnly(2027, 5, 31), SeasonStatus.Open));
        var myTickets = new StubMyTickets();
        myTickets.Emails[Uuid] = ["anna@example.ch"];
        myTickets.Related.Add(new MyTicketSummary(TicketType.EventTicket, Uuid, EventId, TicketStatus.Valid, SwissTime.Timestamp));
        myTickets.Related.Add(new MyTicketSummary(TicketType.SeasonPass, other, 3, TicketStatus.Valid, SwissTime.Timestamp));
        myTickets.Related.Add(new MyTicketSummary(TicketType.SeasonPass, Guid.NewGuid(), 3, TicketStatus.Cancelled, SwissTime.Timestamp));

        var related = await new GetMyTickets.Handler(myTickets, _issued, _tokens, Resolution).HandleAsync(new GetMyTickets.Query(Uuid));

        var pass = Assert.Single(related);
        Assert.Equal("Saison 2026/27", pass.ScopeName);
        Assert.Null(pass.DateText);
        Assert.Equal("Bea Muster", pass.DisplayName);
        Assert.Equal("saison", pass.TypeKey);
    }

    [Fact]
    public async Task Web_ticket_pdf_renders_only_valid_tickets()
    {
        var pdf = new StubTicketPdf();
        var handler = new GetWebTicketPdf.Handler(Resolution, _qr, new StubPublicBaseUrl(), pdf);

        var rendered = await handler.HandleAsync(new GetWebTicketPdf.Query("full-token"));

        Assert.NotNull(rendered);
        Assert.Equal("redants-ticket-11111111.pdf", rendered.FileName);
        Assert.Equal("Red Ants vs. Gegner", pdf.LastModel?.ScopeName);
        Assert.Equal("#C8102E", pdf.LastModel?.AccentHex);

        _issued.Tickets[Uuid] = _issued.Tickets[Uuid] with { Status = TicketStatus.Cancelled };
        Assert.Null(await handler.HandleAsync(new GetWebTicketPdf.Query("full-token")));
    }

    [Fact]
    public async Task Web_ticket_link_is_a_short_token_for_a_known_ticket()
    {
        var handler = new GetWebTicketLink.Handler(_issued, _tokens);

        Assert.Equal("short-" + Uuid.ToString("N")[..8], await handler.HandleAsync(new GetWebTicketLink.Query(Uuid)));
        Assert.Null(await handler.HandleAsync(new GetWebTicketLink.Query(Guid.NewGuid())));
    }

    [Fact]
    public async Task Custom_name_is_set_only_on_a_valid_ticket()
    {
        var names = new RecordingCustomNames();
        var handler = new SetTicketCustomName.Handler(Resolution, names);

        Assert.True(await handler.HandleAsync(new SetTicketCustomName.Command("full-token", "Papa")));
        Assert.Equal((TicketType.EventTicket, Uuid, "Papa"), Assert.Single(names.Calls));
        Assert.False(await handler.HandleAsync(new SetTicketCustomName.Command("bogus", "Papa")));
    }
}

internal sealed class StubTicketTokens : ITicketTokens
{
    public Dictionary<string, TicketTokenData> Full { get; } = new();

    public string Create(TicketType type, Guid uuid, int scopeId) => $"full-{uuid:N}";

    public bool TryVerify(string token, out TicketTokenData data) => Full.TryGetValue(token, out data!);

    public string CreateShort(Guid uuid) => "short-" + uuid.ToString("N")[..8];

    public bool TryVerifyShort(string token, out string code)
    {
        code = token.StartsWith("short-") ? token["short-".Length..] : "";
        return code.Length == 8;
    }
}

internal sealed class StubEvents : IEventReader
{
    public List<Event> Events { get; } = [];

    public Task<IReadOnlyList<Event>> GetAllAsync() => Task.FromResult<IReadOnlyList<Event>>(Events);

    public Task<IReadOnlyList<Event>> GetPublicOpenAsync() =>
        Task.FromResult<IReadOnlyList<Event>>(Events.Where(e => e.Status == EventStatus.Open).ToList());

    public Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync() => Task.FromResult<IReadOnlyList<Event>>(Events);

    public Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<Event>>(Events.Where(e => e.SeasonId == seasonId).ToList());

    public Task<Event?> FindByIdAsync(int id) => Task.FromResult(Events.FirstOrDefault(e => e.Id == id));
}

internal sealed class StubSeasons : ISeasonReader
{
    public List<Season> Seasons { get; } = [];

    public Task<IReadOnlyList<Season>> GetAllAsync() => Task.FromResult<IReadOnlyList<Season>>(Seasons);

    public Task<IReadOnlyList<Season>> GetPublicOpenAsync() =>
        Task.FromResult<IReadOnlyList<Season>>(Seasons.Where(s => s.Status == SeasonStatus.Open).ToList());

    public Task<Season?> FindByIdAsync(int id) => Task.FromResult(Seasons.FirstOrDefault(s => s.Id == id));
}

internal sealed class StubContentUrls : IContentUrls
{
    public string? GetUrl(int nodeId, bool absolute = false) => (absolute ? "https://tickets.test" : "") + $"/node/{nodeId}/";
}

internal sealed class StubQr : IQrCodeRenderer
{
    public string RenderSvg(string content, int pixelsPerModule = 6) => "svg:" + content;

    public string RenderPngDataUri(string content, int pixelsPerModule = 6) => "data:" + content;

    public byte[] RenderPng(string content, int pixelsPerModule = 6) => [(byte)pixelsPerModule];
}

internal sealed class StubMyTickets : IMyTicketsReader
{
    public Dictionary<Guid, IReadOnlyList<string>> Emails { get; } = new();
    public List<MyTicketSummary> Related { get; } = [];

    public Task<IReadOnlyList<string>> FindIdentityEmailsAsync(Guid uuid) => Task.FromResult(Emails.GetValueOrDefault(uuid) ?? []);

    public Task<IReadOnlyList<MyTicketSummary>> GetRelatedAsync(IReadOnlyCollection<string> emails) =>
        Task.FromResult<IReadOnlyList<MyTicketSummary>>(Related);
}

internal sealed class StubTicketPdf : ITicketPdf
{
    public TicketPdfModel? LastModel { get; private set; }

    public byte[] Render(TicketPdfModel model)
    {
        LastModel = model;
        return [1, 2, 3];
    }
}

internal sealed class RecordingCustomNames : ITicketCustomNames
{
    public List<(TicketType Type, Guid Uuid, string? Name)> Calls { get; } = [];

    public Task SetAsync(TicketType type, Guid uuid, string? customName)
    {
        Calls.Add((type, uuid, customName));
        return Task.CompletedTask;
    }
}
