using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed class MyTicketsController(
    IMyTicketTokens myTokens,
    IMyTicketsReader reader,
    ITicketTokens tokens,
    IEvents events,
    ISeasons seasons) : Controller
{
    [HttpGet("/my-tickets/{token}")]
    public async Task<IActionResult> Index(string token)
    {
        if (!myTokens.TryVerify(token, out var email))
            return View("~/Views/MyTickets/Index.cshtml", MyTicketsViewModel.Invalid());

        var summaries = await reader.GetByEmailAsync(email);
        var entries = new List<MyTicketEntry>();

        foreach (var s in summaries)
        {
            var (scopeName, dateText) = await ResolveScopeAsync(s);
            entries.Add(new MyTicketEntry(
                s.Type,
                s.Uuid,
                tokens.CreateShort(s.Uuid),
                scopeName,
                TicketDisplay.TypeLabel(s.Type),
                TicketDisplay.Kicker(s.Type),
                s.Status,
                s.CreatedAt,
                dateText));
        }

        return View("~/Views/MyTickets/Index.cshtml", new MyTicketsViewModel(true, email, entries));
    }

    private async Task<(string ScopeName, string? DateText)> ResolveScopeAsync(MyTicketSummary s)
    {
        if (s.Type == TicketType.EventTicket)
        {
            var ev = await events.FindByIdAsync(s.ScopeId);
            if (ev is null) return ("Anlass", null);
            var d = ev.TimeUnknown ? $"{ev.Date:dd.MM.yyyy}" : $"{ev.Date:dd.MM.yyyy}, {ev.StartTime:HH:mm} Uhr";
            return (ev.Name, d);
        }
        var season = await seasons.FindByIdAsync(s.ScopeId);
        return season is null
            ? ("Saison", null)
            : (season.Name, $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}");
    }
}

public sealed record MyTicketsViewModel(
    bool Found,
    string? Email = null,
    IReadOnlyList<MyTicketEntry>? Tickets = null)
{
    public static MyTicketsViewModel Invalid() => new(false);
}

public sealed record MyTicketEntry(
    TicketType Type,
    Guid Uuid,
    string Token,
    string ScopeName,
    string TypeLabel,
    string Kicker,
    TicketStatus Status,
    DateTime CreatedAt,
    string? DateText);
