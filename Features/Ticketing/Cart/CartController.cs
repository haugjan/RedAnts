using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Cart;

public sealed class CartController(
    ICartService cart, IEventPricing pricing, IEvents events,
    ISeasonPassPricing passPricing, ISeasons seasons) : Controller
{
    [HttpGet("/warenkorb")]
    public IActionResult Index() => View(cart.Get());

    [HttpPost("/warenkorb/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, TicketCategory category, int quantity, string? returnUrl)
    {
        if (quantity < 1) quantity = 1;

        var available = await pricing.FindAvailableAsync(eventId, category);
        var evt = await events.FindByIdAsync(eventId);
        var added = available is { Available: true } && evt is not null;
        if (added)
            cart.Add(eventId, evt!.Name, available!.Category, available.Name, available.Price, quantity);

        if (IsFetchRequest())
        {
            var current = cart.Get();
            return Json(new
            {
                ok = added,
                added = added ? quantity : 0,
                categoryName = available?.Name ?? category.ToString(),
                totalQuantity = current.TotalQuantity,
                totalAmount = current.TotalAmount
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("/warenkorb/add-saisonkarte")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSeasonPass(int seasonId, TicketCategory category, int quantity, string? returnUrl)
    {
        if (quantity < 1) quantity = 1;

        var available = (await passPricing.GetAvailableAsync(seasonId))
            .FirstOrDefault(c => c.Category == category);
        var season = await seasons.FindByIdAsync(seasonId);
        var added = available is { Available: true } && season is not null;
        if (added)
            cart.AddSeasonPass(seasonId, season!.Name, available!.Category, available.Name, available.Price, quantity);

        if (IsFetchRequest())
        {
            var current = cart.Get();
            return Json(new
            {
                ok = added,
                added = added ? quantity : 0,
                categoryName = available?.Name ?? category.ToString(),
                totalQuantity = current.TotalQuantity,
                totalAmount = current.TotalAmount
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("/warenkorb/update")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(string key, int quantity)
    {
        cart.SetQuantity(key, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/warenkorb/remove")]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(string key)
    {
        cart.Remove(key);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/warenkorb/clear")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        cart.Clear();
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectBack(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));

    private bool IsFetchRequest() =>
        Request.Headers.TryGetValue("X-Requested-With", out var xrw) &&
        string.Equals(xrw.ToString(), "fetch", StringComparison.OrdinalIgnoreCase);
}
