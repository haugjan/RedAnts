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
        var open = await events.GetPublicOpenAsync();
        var todays = open.Where(e => e.Date == today).ToList();

        if (todays.Count == 1)
        {
            using var _ = contextFactory.EnsureUmbracoContext();
            var url = urlProvider.GetUrl(todays[0].Id, UrlMode.Relative);
            if (!string.IsNullOrEmpty(url) && url != "#") return Redirect(url);
        }

        return Redirect("/ticketing/");
    }
}
