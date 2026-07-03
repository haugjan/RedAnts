using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
// PaymentMethod exists in both RedAnts.Domain.Ticketing and ...Sales; the sales one is authoritative here.
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Features.Ticketing.Cart;

/// <summary>Guest checkout: capture the billing address (step 1), pick a payment method (step 2), then
/// finalise. Payment is a <b>pseudo</b> step: no gateway is called and every sale succeeds. The order
/// and its issued event tickets are persisted, the cart is cleared, and a confirmation is shown.</summary>
public sealed class CheckoutController(ICartService cart, IOrders orders, IEventTickets tickets) : Controller
{
    private const string FormKey = "RedAnts.Checkout.Form";
    private const string ConfirmationKey = "RedAnts.Checkout.Confirmation";

    // Sports entry fees are VAT-exempt (Art. 21 Abs. 2 Ziff. 15 MWSTG); the club is not VAT-liable here.
    private const decimal VatRate = 0m;

    private static readonly PaymentOption[] Methods =
    [
        new(PaymentMethod.Payrexx, "Kredit-/Debitkarte", "Online-Zahlung (Payrexx)"),
        new(PaymentMethod.Twint, "TWINT", "Mit der TWINT-App bezahlen"),
        new(PaymentMethod.Invoice, "Rechnung", "Zahlung auf Rechnung"),
        new(PaymentMethod.Cash, "Barzahlung", "Vor Ort an der Kasse")
    ];

    // ── Step 1: billing address ───────────────────────────────────────────
    [HttpGet("/kasse")]
    public IActionResult Address()
    {
        if (cart.Get().IsEmpty) return Redirect("/warenkorb");
        return View("~/Views/Checkout/Address.cshtml",
            new CheckoutAddressView { Form = LoadForm() ?? new CheckoutForm(), Cart = cart.Get() });
    }

    [HttpPost("/kasse")]
    [ValidateAntiForgeryToken]
    public IActionResult Address(CheckoutForm form)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/warenkorb");

        try
        {
            // Validate by building the domain value object; discard the result, we only need the check.
            _ = ToBillingAddress(form);
        }
        catch (DomainException ex)
        {
            return View("~/Views/Checkout/Address.cshtml",
                new CheckoutAddressView { Form = form, Cart = current, Error = ex.Message });
        }

        SaveForm(form);
        return Redirect("/kasse/zahlung");
    }

    // ── Step 2: payment method ────────────────────────────────────────────
    [HttpGet("/kasse/zahlung")]
    public IActionResult Payment()
    {
        if (cart.Get().IsEmpty) return Redirect("/warenkorb");
        var form = LoadForm();
        if (form is null) return Redirect("/kasse");
        return View("~/Views/Checkout/Payment.cshtml",
            new CheckoutPaymentView { Cart = cart.Get(), Form = form, Methods = Methods });
    }

    [HttpPost("/kasse/zahlung")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PaymentMethod paymentMethod)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/warenkorb");
        var form = LoadForm();
        if (form is null) return Redirect("/kasse");

        BillingAddress billing;
        try { billing = ToBillingAddress(form); }
        catch (DomainException) { return Redirect("/kasse"); }

        // Pseudo payment: no gateway, always successful. Persist the order as paid.
        var number = await orders.NextOrderNumberAsync();
        var order = Order.Create(number, billing, current.TotalAmount, VatRate, paymentMethod, sellerUid: null);
        order.MarkPaid();
        var saved = await orders.SaveAsync(order);

        // Issue one event ticket per unit; each gets its own Uuid → its own online ticket.
        var issued = new List<ConfirmationTicket>();
        foreach (var item in current.Items)
        {
            for (var i = 0; i < item.Quantity; i++)
            {
                var ticket = await tickets.SaveAsync(
                    EventTicket.Create(item.EventId, item.Category, item.UnitPrice, saved.Id));
                issued.Add(new ConfirmationTicket(ticket.Uuid, item.EventName, item.CategoryName));
            }
        }

        cart.Clear();
        HttpContext.Session.Remove(FormKey);
        SaveConfirmation(new CheckoutConfirmationView
        {
            OrderNumber = saved.OrderNumber,
            Email = billing.Email,
            Total = saved.TotalGross,
            PaymentLabel = Methods.FirstOrDefault(m => m.Method == paymentMethod)?.Label ?? paymentMethod.ToString(),
            Tickets = issued
        });
        return Redirect("/kasse/bestaetigung");
    }

    // ── Confirmation ──────────────────────────────────────────────────────
    [HttpGet("/kasse/bestaetigung")]
    public IActionResult Confirmation()
    {
        var json = HttpContext.Session.GetString(ConfirmationKey);
        if (string.IsNullOrEmpty(json)) return Redirect("/");
        var view = JsonSerializer.Deserialize<CheckoutConfirmationView>(json);
        return view is null ? Redirect("/") : View("~/Views/Checkout/Confirmation.cshtml", view);
    }

    private static BillingAddress ToBillingAddress(CheckoutForm f) => BillingAddress.Create(
        f.FirstName, f.LastName, f.Street, f.AddressLine2, f.PostalCode, f.City, f.Country, f.Email, f.Phone);

    private CheckoutForm? LoadForm()
    {
        var json = HttpContext.Session.GetString(FormKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<CheckoutForm>(json);
    }

    private void SaveForm(CheckoutForm form) =>
        HttpContext.Session.SetString(FormKey, JsonSerializer.Serialize(form));

    private void SaveConfirmation(CheckoutConfirmationView view) =>
        HttpContext.Session.SetString(ConfirmationKey, JsonSerializer.Serialize(view));
}
