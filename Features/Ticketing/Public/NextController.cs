using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Public;

public sealed class NextController(IEvents events, IVenues venues, IEventPricing pricing, IContentUrls contentUrls) : Controller
{
    [HttpGet("/next")]
    public async Task<IActionResult> Next()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();

        var target = upcoming.FirstOrDefault(e => e.Date == today) ?? upcoming.FirstOrDefault();
        if (target is null)
            return View("~/Views/NextQuickBuy.cshtml", NextQuickBuyModel.None);

        var cats = await pricing.GetAvailableAsync(target.Id);
        var venue = await venues.FindByIdAsync(target.VenueId);
        var model = new NextQuickBuyModel(
            target.Id, target.Name, target.ImageUrl,
            target.HomeTeamLogoUrl, target.AwayTeamLogoUrl,
            target.Date, target.StartTime, target.TimeUnknown,
            venue?.Name, cats);
        return View("~/Views/NextQuickBuy.cshtml", model);
    }

    [HttpGet("/next/embed")]
    public async Task<IActionResult> Embed()
    {
        Response.Headers["Content-Security-Policy"] = "frame-ancestors *";

        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();

        var target = upcoming.FirstOrDefault(e => e.Date == today) ?? upcoming.FirstOrDefault();
        if (target is null)
            return View("~/Views/NextEventEmbed.cshtml", NextEventEmbedModel.None);

        var url = contentUrls.GetUrl(target.Id, absolute: true);
        var ticketsUrl = !string.IsNullOrEmpty(url) ? url : "/ticketing/";

        var model = new NextEventEmbedModel(
            target.Name, target.HomeTeamLogoUrl, target.AwayTeamLogoUrl,
            target.Date, target.StartTime, target.TimeUnknown, ticketsUrl);
        return View("~/Views/NextEventEmbed.cshtml", model);
    }
}

public sealed record NextEventEmbedModel(
    string Title, string? HomeLogoUrl, string? AwayLogoUrl,
    DateOnly Date, TimeOnly StartTime, bool TimeUnknown, string TicketsUrl)
{
    public static readonly NextEventEmbedModel None =
        new("", null, null, default, default, false, "/ticketing/");

    public bool HasEvent => !string.IsNullOrEmpty(Title);
}

public sealed record NextQuickBuyModel(
    int EventId, string Title, string? ImageUrl,
    string? HomeLogoUrl, string? AwayLogoUrl,
    DateOnly Date, TimeOnly StartTime, bool TimeUnknown,
    string? VenueName, IReadOnlyList<AvailableTicketCategory> Categories)
{
    public static readonly NextQuickBuyModel None =
        new(0, "", null, null, null, default, default, false, null, []);

    public bool HasEvent => EventId > 0;
}
