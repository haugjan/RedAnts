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
    [HttpPost("/warenkorb/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, TicketCategory category, int quantity, string? returnUrl)
    {
        if (quantity < 1) quantity = 1;

        // Resolve name + price from the catalog server-side; never trust a posted price. Only add when
        // the category is still available (its quota and the event's total sales quota are not exhausted).
        var available = await pricing.FindAvailableAsync(eventId, category);
        var evt = await events.FindByIdAsync(eventId);
        if (available is { Available: true } && evt is not null)
            cart.Add(eventId, evt.Name, available.Category, available.Name, available.Price, quantity);

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
}
