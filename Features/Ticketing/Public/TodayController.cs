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
}
