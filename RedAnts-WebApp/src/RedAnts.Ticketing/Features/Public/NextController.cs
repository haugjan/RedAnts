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
        var entered = ParseQuantities(TempData["QuickQuantities"] as string);
        var siteKey = (await checkoutSettings.HandleAsync(new GetCheckoutSettings.Query())).TurnstileSiteKey;

        if (target is null)
            return View("NextQuickBuy",
                NextQuickBuyModel.None with { Error = error, Email = email, TurnstileSiteKey = siteKey });

        var model = new NextQuickBuyModel(
            target.Id, target.Name,
            target.HomeTeamLogoUrl, target.AwayTeamLogoUrl,
            target.Date, target.StartTime, target.TimeUnknown,
            target.VenueName, target.Categories,
            entered.Count > 0 ? entered : FirstCategoryPreselected(target.Categories),
            error, email, siteKey, target.Url);
        return View("NextQuickBuy", model);
    }

    private static Dictionary<int, int> ParseQuantities(string? stored) => (stored ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split(':'))
        .Where(pair => pair.Length == 2 && int.TryParse(pair[0], out _) && int.TryParse(pair[1], out _))
        .ToDictionary(pair => int.Parse(pair[0]), pair => int.Parse(pair[1]));

    private static Dictionary<int, int> FirstCategoryPreselected(IReadOnlyList<AvailableTicketCategory> categories)
    {
        var first = categories.FirstOrDefault(c => c.Available);
        return first is null ? [] : new Dictionary<int, int> { [first.TierId] = 1 };
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
    int EventId, string Title,
    string? HomeLogoUrl, string? AwayLogoUrl,
    DateOnly Date, TimeOnly StartTime, bool TimeUnknown,
    string? VenueName, IReadOnlyList<AvailableTicketCategory> Categories,
    IReadOnlyDictionary<int, int> Quantities,
    string? Error = null, string Email = "", string? TurnstileSiteKey = null,
    string? EventUrl = null)
{
    public static readonly NextQuickBuyModel None =
        new(0, "", null, null, default, default, false, null, [], new Dictionary<int, int>());

    public bool HasEvent => EventId > 0;

    public int MaxPerCategory => GetQuickBuyCart.MaxPerCategory;

    public int QuantityOf(int tierId) => Quantities.TryGetValue(tierId, out var quantity) ? quantity : 0;

    public int LimitOf(AvailableTicketCategory category) =>
        category.Remaining is { } remaining ? Math.Min(MaxPerCategory, Math.Max(0, remaining)) : MaxPerCategory;

    public decimal Total => Categories
        .Where(c => c.Available)
        .Sum(c => c.Price * QuantityOf(c.TierId));

    public int TotalQuantity => Categories
        .Where(c => c.Available)
        .Sum(c => QuantityOf(c.TierId));
}
