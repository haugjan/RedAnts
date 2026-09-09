using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed class WebTicketController(
    GetWebTicket.Handler getTicket,
    GetMyTickets.Handler getMyTickets,
    GetWebTicketPdf.Handler getPdf,
    GetWebTicketLink.Handler getLink,
    SetTicketCustomName.Handler setCustomName,
    ILogger<WebTicketController> logger) : Controller
{
    [HttpGet("/ticket/{token}")]
    public async Task<IActionResult> Show(string token)
    {
        var ticket = await getTicket.HandleAsync(new GetWebTicket.Query(token));
        if (ticket is null)
            return View("WebTicket", WebTicketViewModel.Invalid());

        var related = ticket.Found ? await getMyTickets.HandleAsync(new GetMyTickets.Query(ticket.Uuid)) : [];

        var model = new WebTicketViewModel(
            Found: ticket.Found,
            Valid: ticket.Valid,
            Kicker: ticket.Kicker,
            TypeLabel: ticket.TypeLabel,
            ScopeName: ticket.ScopeName,
            DateText: ticket.DateText,
            CategoryLabel: ticket.CategoryLabel,
            HolderName: ticket.HolderName,
            TicketRef: ticket.TicketRef,
            QrSvg: ticket.QrSvg,
            HomeLogo: ticket.HomeLogo,
            AwayLogo: ticket.AwayLogo,
            TypeKey: ticket.TypeKey,
            Token: token,
            VenueName: ticket.VenueName,
            Admissions: ticket.Admissions,
            CustomName: ticket.CustomName,
            HolderDefault: ticket.HolderDefault,
            NextMatch: ticket.Upcoming.FirstOrDefault(),
            RelatedTickets: related);

        return View("WebTicket", model);
    }

    [HttpGet("/ticket/{token}/events")]
    public async Task<IActionResult> Events(string token)
    {
        var ticket = await getTicket.HandleAsync(new GetWebTicket.Query(token));
        if (ticket is null)
            return View("WebTicketEvents", TicketEventsViewModel.Invalid());

        return View("WebTicketEvents", new TicketEventsViewModel(
            Found: true,
            Token: token,
            HolderName: ticket.HolderName,
            TypeLabel: ticket.TypeLabel,
            Matches: ticket.Upcoming));
    }

    [HttpPost("/ticket/{token}/name")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetName(string token, string? customName)
    {
        if (!await setCustomName.HandleAsync(new SetTicketCustomName.Command(token, customName))) return NotFound();
        return RedirectToAction(nameof(Show), new { token });
    }

    [HttpGet("/ticket/{token}/manifest.webmanifest")]
    public async Task<IActionResult> Manifest(string token)
    {
        if (await getTicket.HandleAsync(new GetWebTicket.Query(token)) is null) return NotFound();

        var manifest = new
        {
            name = "Red Ants Ticket",
            short_name = "Red Ants",
            description = "Dein Red Ants Online-Ticket.",
            start_url = $"/ticket/{token}",
            scope = "/ticket/",
            display = "standalone",
            orientation = "portrait",
            background_color = "#f4f4f5",
            theme_color = "#C8102E",
            lang = "de",
            icons = new object[]
            {
                new { src = "/icons/ticket-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = "/icons/ticket-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
                new { src = "/icons/ticket-192.png", sizes = "192x192", type = "image/png", purpose = "maskable" },
                new { src = "/icons/ticket-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" }
            }
        };

        var json = JsonSerializer.Serialize(manifest);
        return Content(json, "application/manifest+json");
    }

    [HttpGet("/ticket/{token}/qr.png")]
    public async Task<IActionResult> QrPng(string token)
    {
        var ticket = await getTicket.HandleAsync(new GetWebTicket.Query(token));
        if (ticket is not { Valid: true }) return NotFound();
        return File(ticket.QrPng, "image/png");
    }

    [HttpGet("/ticket/{token}/pdf")]
    public async Task<IActionResult> Pdf(string token)
    {
        WebTicketPdf? pdf;
        try
        {
            pdf = await getPdf.HandleAsync(new GetWebTicketPdf.Query(token));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ticket PDF render failed for token {Token}.", token);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Das Ticket-PDF konnte gerade nicht erzeugt werden. Bitte versuche es in einem Moment erneut.");
        }

        if (pdf is null) return NotFound();
        return File(pdf.Bytes, "application/pdf", pdf.FileName);
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("/ticket/for/{uuid:guid}")]
    public async Task<IActionResult> ForUuid(Guid uuid)
    {
        var token = await getLink.HandleAsync(new GetWebTicketLink.Query(uuid));
        if (token is null) return NotFound();
        return RedirectToAction(nameof(Show), new { token });
    }
}

public sealed record WebTicketViewModel(
    bool Found,
    bool Valid,
    string Kicker,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string TicketRef,
    string QrSvg,
    string? HomeLogo = null,
    string? AwayLogo = null,
    string TypeKey = "spiel",
    string Token = "",
    string? VenueName = null,
    int Admissions = 1,
    string? CustomName = null,
    string? HolderDefault = null,
    UpcomingMatch? NextMatch = null,
    IReadOnlyList<RelatedTicket>? RelatedTickets = null)
{
    public static WebTicketViewModel Invalid() =>
        new(false, false, "", "Ticket", "", null, null, null, "", "");
}

public sealed record TicketEventsViewModel(
    bool Found,
    string Token,
    string? HolderName,
    string TypeLabel,
    IReadOnlyList<UpcomingMatch> Matches)
{
    public static TicketEventsViewModel Invalid() =>
        new(false, "", null, "Ticket", []);
}
