using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Features.Ticketing.Cart;

public sealed class CheckoutController(ICartService cart, IOrders orders, IEventTickets tickets, IOrderMailer mailer, IEventPricing pricing, ITicketTokens tokens, ICaptchaVerifier captcha, ISeasonPasses passes, ISeasonPassPricing passPricing, IPublicBaseUrl publicUrl, IOrderLog orderLog, INewsletterSignups newsletter, IOrderAddOns orderAddOns, IAddOnNotifier addOnNotifier, IPayrexxGateway payrexx, ILogger<CheckoutController> logger) : Controller
{
    private const string FormKey = "RedAnts.Checkout.Form";
    private const string ConfirmationKey = "RedAnts.Checkout.Confirmation";

    private const decimal VatRate = 0m;

    private const string PaymentLabelText = "Online-Zahlung (Payrexx)";

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
                Cart = cart.Get(), Form = form, PayrexxEnabled = payrexx.Enabled,
                TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
                Error = TempData["CheckoutError"] as string
            });
    }

    [HttpPost("/kasse/zahlung")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(bool acceptPrivacy)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/warenkorb");
        var form = LoadForm();
        if (form is null) return Redirect("/kasse");

        if (!acceptPrivacy)
        {
            TempData["CheckoutError"] = "Bitte akzeptiere die AGB und die Datenschutzerklärung.";
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

        return await FinalizeOrderAsync(current, billing, PaymentMethod.Payrexx, form.AcceptNewsletter, "Kasse");
    }

    [HttpGet("/kasse/express")]
    public IActionResult Express()
    {
        if (cart.Get().IsEmpty) return Redirect("/ticketing/");
        if (!ExpressCheckout.IsAllowed(cart.Get())) return Redirect("/kasse");
        return View("~/Views/Checkout/Express.cshtml", new CheckoutExpressView
        {
            Cart = cart.Get(), PayrexxEnabled = payrexx.Enabled,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = TempData["CheckoutError"] as string
        });
    }

    [HttpPost("/kasse/express")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressPay(string email, string? name, bool acceptNewsletter, bool acceptPrivacy)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!ExpressCheckout.IsAllowed(current)) return Redirect("/kasse");

        CheckoutExpressView Invalid(string error) => new()
        {
            Cart = current, PayrexxEnabled = payrexx.Enabled,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = error, Email = email ?? "", Name = name ?? ""
        };

        email = (email ?? "").Trim();
        if (email.Length < 5 || !email.Contains('@') || !email.Contains('.'))
            return View("~/Views/Checkout/Express.cshtml", Invalid("Bitte eine gültige E-Mail-Adresse angeben."));

        if (!acceptPrivacy)
            return View("~/Views/Checkout/Express.cshtml", Invalid("Bitte akzeptiere die AGB und die Datenschutzerklärung."));

        var captchaToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
            return View("~/Views/Checkout/Express.cshtml", Invalid("Bitte bestätige, dass du kein Roboter bist."));

        var trimmed = (name ?? "").Trim();
        var space = trimmed.IndexOf(' ');
        var firstName = space > 0 ? trimmed[..space] : trimmed;
        var lastName = space > 0 ? trimmed[(space + 1)..] : "";
        var billing = BillingAddress.FromPersistence((int)BuyerType.Private, firstName, lastName, null,
            "", null, "", "", "Schweiz", email, null);

        return await FinalizeOrderAsync(current, billing, PaymentMethod.Payrexx, acceptNewsletter, "Express");
    }

    private async Task<IActionResult> FinalizeOrderAsync(Cart current, BillingAddress billing, PaymentMethod paymentMethod, bool subscribeNewsletter, string newsletterSource)
    {
        var demand = current.Items
            .Where(i => i.Kind == CartItemKind.EventTicket)
            .Select(i => new TicketDemand(i.EventId, i.TierId, i.Quantity))
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
        order.SetFulfillmentPayload(BuildSnapshotJson(current, subscribeNewsletter, newsletterSource));
        var saved = await orders.SaveAsync(order);
        await orderLog.AppendAsync(saved.Id, OrderStatus.Draft, "Online-Kauf", "Bestellung erstellt");

        if (payrexx.Enabled)
        {
            var baseUrl = publicUrl.Resolve(Request);
            var request = new PayrexxCreateRequest(
                AmountInCents: (int)Math.Round(saved.TotalGross * 100m, MidpointRounding.AwayFromZero),
                Currency: saved.Currency,
                Purpose: $"Red Ants Ticketing {saved.OrderNumber}",
                ReferenceId: saved.OrderNumber,
                SuccessUrl: $"{baseUrl}/kasse/erfolg?order={saved.Id}",
                FailedUrl: $"{baseUrl}/kasse/zahlung",
                CancelUrl: $"{baseUrl}/kasse/abbruch",
                Email: billing.Email,
                FirstName: billing.FirstName,
                LastName: billing.LastName);
            try
            {
                var gateway = await payrexx.CreateGatewayAsync(request);
                saved.SetPayrexxGatewayId(gateway.GatewayId);
                await orders.SaveAsync(saved);
                return Redirect(gateway.Link);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payrexx gateway creation failed for order {Order}.", saved.OrderNumber);
                TempData["CheckoutError"] = "Die Zahlung konnte nicht gestartet werden. Bitte versuche es erneut.";
                return Redirect(newsletterSource == "Express" ? "/kasse/express" : "/kasse/zahlung");
            }
        }

        var issued = await FulfillAsync(saved.Id);
        cart.Clear();
        HttpContext.Session.Remove(FormKey);
        SaveConfirmation(new CheckoutConfirmationView
        {
            OrderNumber = saved.OrderNumber,
            Email = billing.Email,
            Total = saved.TotalGross,
            PaymentLabel = PaymentLabelText,
            Tickets = issued
        });
        return Redirect("/kasse/bestaetigung");
    }

    private static string BuildSnapshotJson(Cart cart, bool subscribeNewsletter, string newsletterSource)
    {
        var items = cart.Items
            .Select(i => new FulfillmentItem((int)i.Kind, i.EventId, i.SeasonId, i.TierId, i.UnitPrice, i.Quantity, i.EventName, i.CategoryName))
            .ToList();
        var addOns = cart.Items
            .Where(i => i.Kind == CartItemKind.SeasonPass && i.AddOns.Count > 0)
            .SelectMany(i => i.AddOns.Select(a => new FulfillmentAddOn(i.SeasonId, i.EventName, i.TierId, i.CategoryName, a.Label, a.Price, i.Quantity)))
            .Concat(cart.OrderAddOns.Select(a => new FulfillmentAddOn(a.SeasonId, a.SeasonName, 0, "", a.Label, a.Price, 1)))
            .ToList();
        return JsonSerializer.Serialize(new FulfillmentSnapshot(items, addOns, subscribeNewsletter, newsletterSource));
    }

    private async Task<List<ConfirmationTicket>> FulfillAsync(int orderId)
    {
        var order = await orders.GetByIdAsync(orderId);
        if (order is null || order.Status != OrderStatus.Draft || string.IsNullOrEmpty(order.FulfillmentPayload))
            return [];
        if (!await orders.TryMarkPaidAsync(orderId)) return [];
        await orderLog.AppendAsync(order.Id, OrderStatus.Paid, "Online-Kauf", "Online bezahlt");

        var snapshot = JsonSerializer.Deserialize<FulfillmentSnapshot>(order.FulfillmentPayload);
        if (snapshot is null) return [];
        var billing = order.BillingAddress;
        var buyer = billing.ToBuyer();

        var issued = new List<ConfirmationTicket>();
        var mailTickets = new List<OrderMailTicket>();
        foreach (var item in snapshot.Items)
        {
            for (var i = 0; i < item.Quantity; i++)
            {
                if (item.Kind == (int)CartItemKind.SeasonPass)
                {
                    var pass = await passes.SaveAsync(
                        SeasonPass.Create(item.SeasonId, default, item.UnitPrice, order.Id, buyer, "Online-Kauf", tierId: item.TierId));
                    var passToken = tokens.Create(TicketType.SeasonPass, pass.Uuid, item.SeasonId);
                    issued.Add(new ConfirmationTicket(pass.Uuid, item.EventName, item.CategoryName, passToken));
                    mailTickets.Add(new OrderMailTicket(
                        TicketType.SeasonPass, pass.Uuid, item.SeasonId, item.EventName, item.CategoryName));
                    continue;
                }

                var ticket = await tickets.SaveAsync(
                    EventTicket.Create(item.EventId, default, item.UnitPrice, order.Id, buyer, "Online-Kauf", tierId: item.TierId));
                var token = tokens.Create(TicketType.EventTicket, ticket.Uuid, item.EventId);
                issued.Add(new ConfirmationTicket(ticket.Uuid, item.EventName, item.CategoryName, token));
                mailTickets.Add(new OrderMailTicket(
                    TicketType.EventTicket, ticket.Uuid, item.EventId, item.EventName, item.CategoryName));
            }
        }

        if (snapshot.AddOns.Count > 0)
        {
            var addOnLines = snapshot.AddOns
                .Select(a => new OrderAddOnLine(a.SeasonId, a.EventName, default, a.CategoryName, a.Label, a.Price, a.Quantity, a.TierId))
                .ToList();
            await orderAddOns.SaveAsync(order.Id, addOnLines);
            await addOnNotifier.NotifyAsync(order.OrderNumber, billing.FullName, billing.Email, addOnLines);
        }

        await mailer.SendTicketsAsync(new OrderMailModel(
            order.OrderNumber, billing.Email, billing.FullName, order.TotalGross,
            publicUrl.Resolve(Request), mailTickets));

        if (snapshot.SubscribeNewsletter)
            await newsletter.SubscribeAsync(billing.Email, billing.FullName, snapshot.NewsletterSource);

        return issued;
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

        if (found.Status == OrderStatus.Draft && payrexx.Enabled && !string.IsNullOrEmpty(found.PayrexxGatewayId))
        {
            var status = await payrexx.GetGatewayStatusAsync(found.PayrexxGatewayId);
            if (status == PayrexxStatus.Confirmed)
            {
                await FulfillAsync(found.Id);
                cart.Clear();
                HttpContext.Session.Remove(FormKey);
                found = await orders.GetByIdAsync(order) ?? found;
            }
            else if (status is PayrexxStatus.Cancelled or PayrexxStatus.Declined)
            {
                return Redirect("/kasse/abbruch");
            }
        }

        return View("~/Views/Checkout/Processing.cshtml", new CheckoutProcessingView
        {
            OrderId = found.Id,
            OrderNumber = found.OrderNumber,
            Email = found.BillingAddress.Email,
            AlreadyPaid = found.Status == OrderStatus.Paid
        });
    }

    [HttpPost("/payrexx/webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        if (!Request.HasFormContentType) return Ok();
        var reference = Request.Form["transaction[referenceId]"].ToString();
        if (string.IsNullOrWhiteSpace(reference)) reference = Request.Form["referenceId"].ToString();
        if (string.IsNullOrWhiteSpace(reference)) return Ok();

        var order = await orders.GetByNumberAsync(reference.Trim());
        if (order is null || order.Status != OrderStatus.Draft || string.IsNullOrEmpty(order.PayrexxGatewayId))
            return Ok();

        try
        {
            var status = await payrexx.GetGatewayStatusAsync(order.PayrexxGatewayId);
            if (status == PayrexxStatus.Confirmed)
                await FulfillAsync(order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payrexx webhook processing failed for order {Order}.", order.OrderNumber);
        }
        return Ok();
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
            var byTier = (await passPricing.GetAvailableAsync(bySeason.Key)).ToDictionary(c => c.TierId);
            foreach (var item in bySeason)
            {
                if (!byTier.TryGetValue(item.TierId, out var cat) || !cat.Available)
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
