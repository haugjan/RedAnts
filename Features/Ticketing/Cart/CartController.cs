using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Cart;

public sealed class CartController(
    ICartService cart, IEventPricing pricing, IEvents events,
    ISeasonPassPricing passPricing, ISeasons seasons, ISeasonAddOns seasonAddOns,
    IPriceTiers priceTiers, IConvertibleCards convertibleCards, IEventConversionRules conversionRules) : Controller
{
    [HttpGet("/cart")]
    public IActionResult Index() => View(cart.Get());

    [HttpPost("/cart/convert")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int eventId, string? cardNumber, string? returnUrl)
    {
        var res = await convertibleCards.ResolveAsync(eventId, cardNumber ?? "");
        var added = false;
        string message;
        if (!res.Ok || res.Offer is null)
        {
            message = res.Error ?? "Umwandlung nicht möglich.";
        }
        else
        {
            var o = res.Offer;
            var cartForEvent = cart.Get().Items
                .Where(i => i.Kind == CartItemKind.EventTicket && i.EventId == eventId)
                .Sum(i => i.Quantity);
            if (o.EventRemaining is { } rem && cartForEvent >= rem)
            {
                message = "Für diesen Anlass sind keine Tickets mehr verfügbar (Kontingent ausgeschöpft).";
            }
            else
            {
                var n = cart.AddConversion(eventId, o.EventName, o.SeasonId, o.TierId, o.CardLabel,
                    o.OriginCategory, o.Price, o.CardType, o.CardUuid, o.CardLabel, o.RemainingCap);
                added = n > 0;
                message = added
                    ? (o.Price > 0m
                        ? $"Umgewandeltes Ticket ({o.CardLabel}) für CHF {o.Price:0.00} im Warenkorb."
                        : $"Umgewandeltes Ticket ({o.CardLabel}) im Warenkorb.")
                    : "Für diese Karte sind bereits alle Umwandlungen im Warenkorb.";
            }
        }

        if (IsFetchRequest())
        {
            var current = cart.Get();
            return Json(new
            {
                ok = added,
                message,
                totalQuantity = current.TotalQuantity,
                totalAmount = current.TotalAmount
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("/cart/direct")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAndCheckout(int eventId, int tierId, int quantity)
    {
        if (quantity < 1) quantity = 1;

        if (!await conversionRules.GetConversionOnlyAsync(eventId))
        {
            var available = await pricing.FindAvailableByTierAsync(eventId, tierId);
            var evt = await events.FindByIdAsync(eventId);
            if (available is { Available: true } && evt is not null)
                cart.Add(eventId, evt.Name, available.TierId, available.Name, StdName(available), available.Price, quantity);
        }

        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/cart");
        return Redirect(ExpressCheckout.IsAllowed(current) ? "/checkout/express" : "/checkout");
    }

    [HttpPost("/cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, int tierId, int quantity, string? returnUrl)
    {
        if (quantity < 1) quantity = 1;

        if (await conversionRules.GetConversionOnlyAsync(eventId))
        {
            if (IsFetchRequest())
                return Json(new { ok = false, added = 0, categoryName = "",
                    totalQuantity = cart.Get().TotalQuantity, totalAmount = cart.Get().TotalAmount,
                    message = "Für diesen Anlass sind normale Ticketkäufe nicht möglich (nur Kartenumwandlung)." });
            return RedirectBack(returnUrl);
        }

        var available = await pricing.FindAvailableByTierAsync(eventId, tierId);
        var evt = await events.FindByIdAsync(eventId);
        var added = available is { Available: true } && evt is not null;
        if (added)
            cart.Add(eventId, evt!.Name, available!.TierId, available.Name, StdName(available), available.Price, quantity);

        if (IsFetchRequest())
        {
            var current = cart.Get();
            return Json(new
            {
                ok = added,
                added = added ? quantity : 0,
                categoryName = available?.Name ?? "",
                totalQuantity = current.TotalQuantity,
                totalAmount = current.TotalAmount
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("/cart/add-season-pass")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSeasonPass(int seasonId, int tierId, int quantity, string? returnUrl, int[]? addOns)
    {
        if (quantity < 1) quantity = 1;

        var available = await passPricing.FindAvailableByTierAsync(seasonId, tierId);
        var season = await seasons.FindByIdAsync(seasonId);
        var added = available is { Available: true } && season is not null;
        if (added)
        {
            var selectedIds = (addOns ?? []).ToHashSet();
            List<SeasonAddOn> chosen;
            if (selectedIds.Count == 0)
                chosen = new List<SeasonAddOn>();
            else
            {
                var tierBase = (await priceTiers.GetBySeasonAsync(seasonId)).ToDictionary(t => t.Id, t => t.PromoOfTierId ?? t.Id);
                var baseTierId = tierBase.GetValueOrDefault(available!.TierId, available.TierId);
                var isPromoOffer = baseTierId != available.TierId;
                chosen = (await seasonAddOns.GetBySeasonAsync(seasonId))
                    .Where(a => a.Active && selectedIds.Contains(a.Id)
                        && (a.AllowedTierIds.Count == 0 || a.AllowedTierIds.Contains(baseTierId))
                        && (!a.PromoOnly || isPromoOffer))
                    .ToList();
            }
            var perPass = chosen.Where(a => a.Scope == AddOnScope.PerPass)
                .Select(a => new CartAddOn { Id = a.Id, Label = a.Label, Price = a.Price })
                .ToList();
            var perOrder = chosen.Where(a => a.Scope == AddOnScope.PerOrder)
                .Select(a => new CartAddOn { Id = a.Id, Label = a.Label, Price = a.Price, SeasonId = seasonId, SeasonName = season!.Name })
                .ToList();
            cart.AddSeasonPass(seasonId, season!.Name, available!.TierId, available.Name, StdName(available), available.Price, quantity, perPass);
            cart.AddOrderAddOns(perOrder);
        }

        if (IsFetchRequest())
        {
            var current = cart.Get();
            return Json(new
            {
                ok = added,
                added = added ? quantity : 0,
                categoryName = available?.Name ?? "",
                totalQuantity = current.TotalQuantity,
                totalAmount = current.TotalAmount
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("/cart/update")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(string key, int quantity)
    {
        cart.SetQuantity(key, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/remove")]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(string key)
    {
        cart.Remove(key);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/remove-addon")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveAddOn(int addOnId)
    {
        cart.RemoveOrderAddOn(addOnId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/clear")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        cart.Clear();
        return RedirectToAction(nameof(Index));
    }

    private static string StdName(AvailableTicketCategory a) =>
        string.IsNullOrWhiteSpace(a.ShortName) ? a.Name : a.ShortName!;

    private IActionResult RedirectBack(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));

    private bool IsFetchRequest() =>
        Request.Headers.TryGetValue("X-Requested-With", out var xrw) &&
        string.Equals(xrw.ToString(), "fetch", StringComparison.OrdinalIgnoreCase);
}
