using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.CheckoutWorkflow;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed class CartController(
    ICartRepository carts,
    AddEventTicketsToCart.Handler addEventTickets,
    AddSeasonPassesToCart.Handler addSeasonPasses,
    AddConversionToCart.Handler addConversion,
    ChangeCartLineQuantity.Handler changeQuantity,
    RemoveOrderAddOnFromCart.Handler removeOrderAddOn,
    ClearCart.Handler clearCart) : Controller
{
    [HttpGet("/cart")]
    public IActionResult Index() => View(carts.Load());

    [HttpPost("/cart/convert")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int eventId, string? cardNumber, int? tierId, string? returnUrl)
    {
        var result = await addConversion.HandleAsync(new AddConversionToCart.Command(eventId, cardNumber ?? "", tierId));
        if (!IsFetchRequest()) return RedirectBack(returnUrl);

        if (result.NeedsTier)
            return Json(new
            {
                ok = false,
                needsTier = true,
                message = result.Message,
                tierChoices = result.TierChoices.Select(c => new { tierId = c.TierId, name = c.Name, price = c.Price })
            });

        return Json(new
        {
            ok = result.Added,
            message = result.Message,
            totalQuantity = result.Cart.TotalQuantity,
            totalAmount = result.Cart.TotalAmount
        });
    }

    [HttpPost("/cart/direct")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAndCheckout(int eventId, int tierId, int quantity)
    {
        var result = await addEventTickets.HandleAsync(new AddEventTicketsToCart.Command(eventId, tierId, quantity));
        if (result.Cart.IsEmpty) return Redirect("/cart");
        return Redirect(result.Cart.QualifiesForExpress ? "/checkout/express" : "/checkout");
    }

    [HttpPost("/cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, int tierId, int quantity, string? returnUrl)
    {
        var result = await addEventTickets.HandleAsync(new AddEventTicketsToCart.Command(eventId, tierId, quantity));
        if (!IsFetchRequest()) return RedirectBack(returnUrl);

        if (result.Message is { } message)
            return Json(new
            {
                ok = false,
                added = 0,
                categoryName = "",
                totalQuantity = result.Cart.TotalQuantity,
                totalAmount = result.Cart.TotalAmount,
                message
            });

        return Json(new
        {
            ok = result.Added,
            added = result.Added ? Math.Max(1, quantity) : 0,
            categoryName = result.CategoryName,
            totalQuantity = result.Cart.TotalQuantity,
            totalAmount = result.Cart.TotalAmount
        });
    }

    [HttpPost("/cart/add-season-pass")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSeasonPass(int seasonId, int tierId, int quantity, string? returnUrl, int[]? addOns)
    {
        var result = await addSeasonPasses.HandleAsync(new AddSeasonPassesToCart.Command(seasonId, tierId, quantity, addOns ?? []));
        if (!IsFetchRequest()) return RedirectBack(returnUrl);

        return Json(new
        {
            ok = result.Added,
            added = result.Added ? Math.Max(1, quantity) : 0,
            categoryName = result.CategoryName,
            totalQuantity = result.Cart.TotalQuantity,
            totalAmount = result.Cart.TotalAmount
        });
    }

    [HttpPost("/cart/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, int quantity)
    {
        await changeQuantity.HandleAsync(new ChangeCartLineQuantity.Command(key, quantity));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string key)
    {
        await changeQuantity.HandleAsync(new ChangeCartLineQuantity.Command(key, 0));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/remove-addon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAddOn(int addOnId)
    {
        await removeOrderAddOn.HandleAsync(new RemoveOrderAddOnFromCart.Command(addOnId));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await clearCart.HandleAsync(new ClearCart.Command());
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
