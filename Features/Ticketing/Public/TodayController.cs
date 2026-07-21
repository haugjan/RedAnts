using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace RedAnts.Features.Ticketing.Public;

public sealed class TodayController(
    IEvents events,
    IPublishedUrlProvider urlProvider,
    IUmbracoContextFactory contextFactory) : Controller
{
    [HttpGet("/heute")]
    public async Task<IActionResult> Today()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();

        var target = upcoming.FirstOrDefault(e => e.Date == today) ?? upcoming.FirstOrDefault();
        if (target is null) return Redirect("/ticketing/");

        using var _ = contextFactory.EnsureUmbracoContext();
        var url = urlProvider.GetUrl(target.Id, UrlMode.Relative);
        return Redirect(!string.IsNullOrEmpty(url) && url != "#" ? url : "/ticketing/");
    }

    [HttpGet("/heute/embed")]
    public async Task<IActionResult> Embed()
    {
        Response.Headers["Content-Security-Policy"] = "frame-ancestors *";

        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();

        var target = upcoming.FirstOrDefault(e => e.Date == today) ?? upcoming.FirstOrDefault();
        if (target is null)
            return View("~/Views/NextEventEmbed.cshtml", NextEventEmbedModel.None);

        using var _ = contextFactory.EnsureUmbracoContext();
        var url = urlProvider.GetUrl(target.Id, UrlMode.Absolute);
        var ticketsUrl = !string.IsNullOrEmpty(url) && url != "#" ? url : "/ticketing/";

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
