using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Public;

public sealed class NextController(IEvents events, IContentUrls contentUrls) : Controller
{
    [HttpGet("/next")]
    public async Task<IActionResult> Next()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();

        var target = upcoming.FirstOrDefault(e => e.Date == today) ?? upcoming.FirstOrDefault();
        if (target is null) return Redirect("/ticketing/");

        var url = contentUrls.GetUrl(target.Id);
        return Redirect(!string.IsNullOrEmpty(url) ? url : "/ticketing/");
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
