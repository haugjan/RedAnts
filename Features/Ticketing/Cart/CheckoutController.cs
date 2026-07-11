using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Features.Ticketing.Cart;

public sealed class CheckoutController(ICartService cart, IOrders orders, IEventTickets tickets, IOrderMailer mailer, IEventPricing pricing, ITicketTokens tokens, ICaptchaVerifier captcha, ISeasonPasses passes, ISeasonPassPricing passPricing, IPublicBaseUrl publicUrl, IOrderLog orderLog, INewsletterSignups newsletter, IOrderAddOns orderAddOns, IAddOnNotifier addOnNotifier) : Controller
{
    private const string FormKey = "RedAnts.Checkout.Form";
    private const string ConfirmationKey = "RedAnts.Checkout.Confirmation";

    private const decimal VatRate = 0m;

    private static readonly PaymentOption[] Methods =
    [
        new(PaymentMethod.Payrexx, "Kredit-/Debitkarte", "Online-Zahlung (Payrexx)"),
        new(PaymentMethod.Twint, "TWINT", "Mit der TWINT-App bezahlen")
    ];

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

    [HttpGet("/kasse/zahlung")]
    public IActionResult Payment()
    {
        if (cart.Get().IsEmpty) return Redirect("/warenkorb");
        var form = LoadForm();
        if (form is null) return Redirect("/kasse");
        return View("~/Views/Checkout/Payment.cshtml",
            new CheckoutPaymentView
            {
                Cart = cart.Get(), Form = form, Methods = Methods,
                TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
                Error = TempData["CheckoutError"] as string
            });
    }

    [HttpPost("/kasse/zahlung")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PaymentMethod paymentMethod, bool acceptPrivacy)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/warenkorb");
        if (Methods.All(m => m.Method != paymentMethod)) return Redirect("/kasse/zahlung");
        var form = LoadForm();
        if (form is null) return Redirect("/kasse");

        if (!acceptPrivacy)
        {
            TempData["CheckoutError"] = "Bitte bestätige, dass du die Datenschutzerklärung gelesen hast.";
            return Redirect("/kasse/zahlung");
        }

        var captchaToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
        {
            TempData["CheckoutError"] = "Bitte bestätige, dass du kein Roboter bist.";
            return Redirect("/kasse/zahlung");
        }

        BillingAddress billing;
        try { billing = ToBillingAddress(form); }
        catch (DomainException) { return Redirect("/kasse"); }

        return await FinalizeOrderAsync(current, billing, paymentMethod, form.AcceptNewsletter, "Kasse");
    }

    [HttpGet("/kasse/express")]
    public IActionResult Express()
    {
        if (cart.Get().IsEmpty) return Redirect("/ticketing/");
        if (!ExpressCheckout.IsAllowed(cart.Get())) return Redirect("/kasse");
        return View("~/Views/Checkout/Express.cshtml", new CheckoutExpressView
        {
            Cart = cart.Get(), Methods = Methods,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = TempData["CheckoutError"] as string
        });
    }

    [HttpPost("/kasse/express")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressPay(string email, string? name, PaymentMethod paymentMethod, bool acceptNewsletter, bool acceptPrivacy)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!ExpressCheckout.IsAllowed(current)) return Redirect("/kasse");
        if (Methods.All(m => m.Method != paymentMethod)) return Redirect("/kasse/express");

        CheckoutExpressView Invalid(string error) => new()
        {
            Cart = current, Methods = Methods,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = error, Email = email ?? "", Name = name ?? ""
        };

        email = (email ?? "").Trim();
        if (email.Length < 5 || !email.Contains('@') || !email.Contains('.'))
            return View("~/Views/Checkout/Express.cshtml", Invalid("Bitte eine gültige E-Mail-Adresse angeben."));

        if (!acceptPrivacy)
            return View("~/Views/Checkout/Express.cshtml", Invalid("Bitte bestätige, dass du die Datenschutzerklärung gelesen hast."));

        var captchaToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
            return View("~/Views/Checkout/Express.cshtml", Invalid("Bitte bestätige, dass du kein Roboter bist."));

        var trimmed = (name ?? "").Trim();
        var space = trimmed.IndexOf(' ');
        var firstName = space > 0 ? trimmed[..space] : trimmed;
        var lastName = space > 0 ? trimmed[(space + 1)..] : "";
        var billing = BillingAddress.FromPersistence((int)BuyerType.Private, firstName, lastName, null,
            "", null, "", "", "Schweiz", email, null);

        return await FinalizeOrderAsync(current, billing, paymentMethod, acceptNewsletter, "Express");
    }

    private async Task<IActionResult> FinalizeOrderAsync(Cart current, BillingAddress billing, PaymentMethod paymentMethod, bool subscribeNewsletter, string newsletterSource)
    {
        var demand = current.Items
            .Where(i => i.Kind == CartItemKind.EventTicket)
            .Select(i => new TicketDemand(i.EventId, i.Category, i.Quantity))
            .ToList();
        var capacityError = await pricing.CheckCapacityAsync(demand);
        capacityError ??= await CheckSeasonPassCapacityAsync(current);
        if (capacityError is not null)
        {
            TempData["CartError"] = capacityError;
            return Redirect("/warenkorb");
        }

        var number = await orders.NextOrderNumberAsync();
        var order = Order.Create(number, billing, current.TotalAmount, VatRate, paymentMethod, sellerUid: null);
        order.MarkPaid();
        var saved = await orders.SaveAsync(order);
        await orderLog.AppendAsync(saved.Id, OrderStatus.Draft, "Online-Kauf", "Bestellung erstellt");
        await orderLog.AppendAsync(saved.Id, OrderStatus.Paid, "Online-Kauf", "Online bezahlt");
        var buyer = billing.ToBuyer();

        var issued = new List<ConfirmationTicket>();
        var mailTickets = new List<OrderMailTicket>();
        foreach (var item in current.Items)
        {
            for (var i = 0; i < item.Quantity; i++)
            {
                if (item.Kind == CartItemKind.SeasonPass)
                {
                    var pass = await passes.SaveAsync(
                        SeasonPass.Create(item.SeasonId, item.Category, item.UnitPrice, saved.Id, buyer, "Online-Kauf"));
                    var passToken = tokens.Create(TicketType.SeasonPass, pass.Uuid, item.SeasonId);
                    issued.Add(new ConfirmationTicket(pass.Uuid, item.EventName, item.CategoryName, passToken));
                    mailTickets.Add(new OrderMailTicket(
                        TicketType.SeasonPass, pass.Uuid, item.SeasonId, item.EventName, item.CategoryName));
                    continue;
                }

                var ticket = await tickets.SaveAsync(
                    EventTicket.Create(item.EventId, item.Category, item.UnitPrice, saved.Id, buyer, "Online-Kauf"));
                var token = tokens.Create(TicketType.EventTicket, ticket.Uuid, item.EventId);
                issued.Add(new ConfirmationTicket(ticket.Uuid, item.EventName, item.CategoryName, token));
                mailTickets.Add(new OrderMailTicket(
                    TicketType.EventTicket, ticket.Uuid, item.EventId, item.EventName, item.CategoryName));
            }
        }

        var addOnLines = current.Items
            .Where(i => i.Kind == CartItemKind.SeasonPass && i.AddOns.Count > 0)
            .SelectMany(i => i.AddOns.Select(a => new OrderAddOnLine(
                i.SeasonId, i.EventName, i.Category, i.CategoryName, a.Label, a.Price, i.Quantity)))
            .ToList();
        if (addOnLines.Count > 0)
        {
            await orderAddOns.SaveAsync(saved.Id, addOnLines);
            await addOnNotifier.NotifyAsync(saved.OrderNumber, billing.FullName, billing.Email, addOnLines);
        }

        await mailer.SendTicketsAsync(new OrderMailModel(
            saved.OrderNumber, billing.Email, billing.FullName, saved.TotalGross,
            publicUrl.Resolve(Request), mailTickets));

        if (subscribeNewsletter)
            await newsletter.SubscribeAsync(billing.Email, billing.FullName, newsletterSource);

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

    [HttpGet("/kasse/bestaetigung")]
    public IActionResult Confirmation()
    {
        var json = HttpContext.Session.GetString(ConfirmationKey);
        if (string.IsNullOrEmpty(json)) return Redirect("/");
        var view = JsonSerializer.Deserialize<CheckoutConfirmationView>(json);
        return view is null ? Redirect("/") : View("~/Views/Checkout/Confirmation.cshtml", view);
    }

    [HttpGet("/kasse/erfolg")]
    public async Task<IActionResult> Processing(int order)
    {
        var found = await orders.GetByIdAsync(order);
        if (found is null) return Redirect("/");
        return View("~/Views/Checkout/Processing.cshtml", new CheckoutProcessingView
        {
            OrderId = found.Id,
            OrderNumber = found.OrderNumber,
            Email = found.BillingAddress.Email,
            AlreadyPaid = found.Status == OrderStatus.Paid
        });
    }

    [HttpGet("/kasse/status")]
    public async Task<IActionResult> Status(int order)
    {
        var found = await orders.GetByIdAsync(order);
        if (found is null) return NotFound();
        return Json(new
        {
            paid = found.Status == OrderStatus.Paid,
            cancelled = found.Status is OrderStatus.Cancelled or OrderStatus.Refunded
        });
    }

    [HttpGet("/kasse/abbruch")]
    public IActionResult Cancelled() => View("~/Views/Checkout/Cancelled.cshtml");

    private async Task<string?> CheckSeasonPassCapacityAsync(Cart cart)
    {
        foreach (var bySeason in cart.Items.Where(i => i.Kind == CartItemKind.SeasonPass).GroupBy(i => i.SeasonId))
        {
            var byCategory = (await passPricing.GetAvailableAsync(bySeason.Key)).ToDictionary(c => c.Category);
            foreach (var item in bySeason)
            {
                if (!byCategory.TryGetValue(item.Category, out var cat) || !cat.Available)
                    return $"{item.CategoryName} ist nicht mehr verfügbar.";
                if (cat.Remaining is { } r && r < item.Quantity)
                    return $"{item.CategoryName} ist nicht mehr in dieser Anzahl verfügbar.";
            }
        }
        return null;
    }

    private static BillingAddress ToBillingAddress(CheckoutForm f) => BillingAddress.Create(
        f.Type, f.FirstName, f.LastName, f.Company,
        f.Street, f.AddressLine2, f.PostalCode, f.City, f.Country, f.Email, f.Phone);

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
