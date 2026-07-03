using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Cart;

/// <summary>Guest shopping cart: add ticket categories, view/adjust the cart. Checkout is out of scope.</summary>
public sealed class CartController(ICartService cart, IEventPricing pricing, IEvents events) : Controller
{
    // GET /warenkorb — cart page.
    [HttpGet("/warenkorb")]
    public IActionResult Index() => View(cart.Get());

    // POST /warenkorb/add — add a quantity of one category for one event.
    // A normal form post redirects back (no-JS fallback); a fetch call (header X-Requested-With: fetch)
    // gets JSON with the new cart totals so the page can give inline feedback without reloading.
    [HttpPost("/warenkorb/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, TicketCategory category, int quantity, string? returnUrl)
    {
        if (quantity < 1) quantity = 1;

        // Resolve name + price from the catalog server-side; never trust a posted price. Only add when
        // the category is still available (its quota and the event's total sales quota are not exhausted).
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

    /// <summary>True when the request came from the page's fetch handler (no full-page redirect wanted).</summary>
    private bool IsFetchRequest() =>
        Request.Headers.TryGetValue("X-Requested-With", out var xrw) &&
        string.Equals(xrw.ToString(), "fetch", StringComparison.OrdinalIgnoreCase);
}
