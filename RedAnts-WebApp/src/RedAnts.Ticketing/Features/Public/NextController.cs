using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Public;

public sealed class NextController(GetNextEvent.Handler nextEvent, GetCheckoutSettings.Handler checkoutSettings) : Controller
{
    [HttpGet("/next")]
    public async Task<IActionResult> Next()
    {
        var target = await nextEvent.HandleAsync(new GetNextEvent.Query());
        var error = TempData["QuickError"] as string;
        var email = TempData["QuickEmail"] as string ?? "";
        var name = TempData["QuickName"] as string ?? "";
        var siteKey = (await checkoutSettings.HandleAsync(new GetCheckoutSettings.Query())).TurnstileSiteKey;

        if (target is null)
            return View("NextQuickBuy",
                NextQuickBuyModel.None with { Error = error, Email = email, Name = name, TurnstileSiteKey = siteKey });

        var model = new NextQuickBuyModel(
            target.Id, target.Name, target.ImageUrl,
            target.HomeTeamLogoUrl, target.AwayTeamLogoUrl,
            target.Date, target.StartTime, target.TimeUnknown,
            target.VenueName, target.Categories, error, email, name, siteKey, target.Url);
        return View("NextQuickBuy", model);
    }

    [HttpGet("/next/embed")]
    public async Task<IActionResult> Embed()
    {
        Response.Headers["Content-Security-Policy"] = "frame-ancestors *";

        var target = await nextEvent.HandleAsync(new GetNextEvent.Query());
        if (target is null)
            return View("NextEventEmbed", NextEventEmbedModel.None);

        var ticketsUrl = !string.IsNullOrEmpty(target.AbsoluteUrl) ? target.AbsoluteUrl : "/ticketing/";

        var model = new NextEventEmbedModel(
            target.Name, target.HomeTeamLogoUrl, target.AwayTeamLogoUrl,
            target.Date, target.StartTime, target.TimeUnknown, ticketsUrl);
        return View("NextEventEmbed", model);
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
    string? VenueName, IReadOnlyList<AvailableTicketCategory> Categories,
    string? Error = null, string Email = "", string Name = "", string? TurnstileSiteKey = null,
    string? EventUrl = null)
{
    public static readonly NextQuickBuyModel None =
        new(0, "", null, null, null, default, default, false, null, []);

    public bool HasEvent => EventId > 0;
}
