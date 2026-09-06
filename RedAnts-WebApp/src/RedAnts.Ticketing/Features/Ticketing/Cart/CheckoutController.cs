using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
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

public sealed class CheckoutController(ICartService cart, IOrders orders, IEventTickets tickets, IOrderMailer mailer, IEventPricing pricing, ITicketTokens tokens, ICaptchaVerifier captcha, ISeasonPasses passes, ISeasonPassPricing passPricing, IPublicBaseUrl publicUrl, IOrderLog orderLog, INewsletterSignups newsletter, IOrderAddOns orderAddOns, IOrderItems orderItems, IAddOnNotifier addOnNotifier, ISeasonAddOns seasonAddOns, IPayrexxGateway payrexx, RedAnts.Features.Ticketing.Scanning.IAdmissionService admission, IEvents events, ISeasons seasons, IVenues venues, IIssuedTicketReader issuedTickets, IConvertibleCards convertibleCards, IEventConversionRules conversionRules, IDataProtectionProvider dataProtection, ILogger<CheckoutController> logger) : Controller
{
    private const string FormKey = "RedAnts.Checkout.Form";
    private const string ConfirmationKey = "RedAnts.Checkout.Confirmation";

    private readonly IDataProtector _orderProtector = dataProtection.CreateProtector("RedAnts.CheckoutOrder.v1");

    private string ProtectOrder(int orderId) => _orderProtector.Protect(orderId.ToString());

    private int? UnprotectOrder(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        try { return int.Parse(_orderProtector.Unprotect(token)); }
        catch { return null; }
    }

    private const decimal VatRate = 0m;

    private const string PaymentLabelText = "Online-Zahlung (Payrexx)";

    [HttpGet("/checkout")]
    public async Task<IActionResult> Address(string? payment = null)
    {
        if (cart.Get().IsEmpty) return Redirect("/cart");
        var error = payment == "aborted"
            ? "Die Zahlung wurde abgebrochen oder ist fehlgeschlagen. Bitte versuche es erneut."
            : TempData["CheckoutError"] as string;
        return await CheckoutView(LoadForm() ?? new CheckoutForm(), error);
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutForm form, bool acceptPrivacy)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/cart");

        SaveForm(form);

        BillingAddress billing;
        try { billing = ToBillingAddress(form); }
        catch (DomainException ex) { return await CheckoutView(form, ex.Message); }

        if (!acceptPrivacy)
            return await CheckoutView(form, "Bitte akzeptiere die AGB und die Datenschutzerklärung.");

        if (string.IsNullOrWhiteSpace(form.Phone) && await CartRequiresMobileAsync(current))
            return await CheckoutView(form, "Für die gewählte Zusatzoption ist deine Mobilnummer zwingend. Bitte gib sie an.");

        var captchaToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
            return await CheckoutView(form, "Bitte bestätige, dass du kein Roboter bist.");

        return await FinalizeOrderAsync(current, billing, PaymentMethod.Payrexx, form.AcceptNewsletter, "Kasse");
    }

    [HttpGet("/checkout/payment")]
    public IActionResult Payment() => Redirect("/checkout");

    private async Task<IActionResult> CheckoutView(CheckoutForm form, string? error)
    {
        var current = cart.Get();
        return View("~/Views/Checkout/Address.cshtml", new CheckoutAddressView
        {
            Form = form,
            Cart = current,
            PayrexxEnabled = payrexx.Enabled,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = error,
            MobileRequired = await CartRequiresMobileAsync(current)
        });
    }

    private async Task<bool> CartRequiresMobileAsync(Cart current)
    {
        var idsBySeason = new Dictionary<int, HashSet<int>>();
        void Track(int seasonId, int addOnId)
        {
            if (!idsBySeason.TryGetValue(seasonId, out var set)) idsBySeason[seasonId] = set = new();
            set.Add(addOnId);
        }
        foreach (var item in current.Items.Where(i => i.Kind == CartItemKind.SeasonPass))
            foreach (var a in item.AddOns) Track(item.SeasonId, a.Id);
        foreach (var a in current.OrderAddOns) Track(a.SeasonId, a.Id);

        foreach (var (seasonId, ids) in idsBySeason)
        {
            var defs = await seasonAddOns.GetBySeasonAsync(seasonId);
            if (defs.Any(d => ids.Contains(d.Id) && d.RequireMobileNumber)) return true;
        }
        return false;
    }

    [HttpGet("/checkout/express")]
    public IActionResult Express()
    {
        if (cart.Get().IsEmpty) return Redirect("/ticketing/");
        if (!ExpressCheckout.IsAllowed(cart.Get())) return Redirect("/checkout");
        return View("~/Views/Checkout/Express.cshtml", new CheckoutExpressView
        {
            Cart = cart.Get(), PayrexxEnabled = payrexx.Enabled,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = TempData["CheckoutError"] as string
        });
    }

    [HttpPost("/checkout/express")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressPay(string email, string? name, bool acceptNewsletter, bool acceptPrivacy)
    {
        var current = cart.Get();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!ExpressCheckout.IsAllowed(current)) return Redirect("/checkout");

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

    [HttpPost("/next/buy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickBuy(int eventId, int tierId, string email, string? name, bool acceptNewsletter, bool acceptPrivacy)
    {
        IActionResult Back(string error)
        {
            TempData["QuickError"] = error;
            TempData["QuickEmail"] = email ?? "";
            TempData["QuickName"] = name ?? "";
            return Redirect("/next");
        }

        email = (email ?? "").Trim();
        if (email.Length < 5 || !email.Contains('@') || !email.Contains('.'))
            return Back("Bitte eine gültige E-Mail-Adresse angeben.");

        if (!acceptPrivacy)
            return Back("Bitte akzeptiere die AGB und die Datenschutzerklärung.");

        var captchaToken = Request.Form["cf-turnstile-response"].ToString();
        if (!await captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
            return Back("Bitte bestätige, dass du kein Roboter bist.");

        var available = await pricing.FindAvailableByTierAsync(eventId, tierId);
        var evt = await events.FindByIdAsync(eventId);
        if (available is not { Available: true } || evt is null)
            return Back("Dieses Ticket ist nicht mehr verfügbar.");

        var oneTicket = new Cart
        {
            Items =
            {
                new CartItem
                {
                    Kind = CartItemKind.EventTicket,
                    EventId = eventId,
                    EventName = evt.Name,
                    TierId = available.TierId,
                    CategoryName = available.Name,
                    UnitPrice = available.Price,
                    Quantity = 1
                }
            }
        };

        var trimmed = (name ?? "").Trim();
        var space = trimmed.IndexOf(' ');
        var firstName = space > 0 ? trimmed[..space] : trimmed;
        var lastName = space > 0 ? trimmed[(space + 1)..] : "";
        var billing = BillingAddress.FromPersistence((int)BuyerType.Private, firstName, lastName, null,
            "", null, "", "", "Schweiz", email, null);

        return await FinalizeOrderAsync(oneTicket, billing, PaymentMethod.Payrexx, acceptNewsletter, "QuickBuy");
    }

    private async Task<IActionResult> FinalizeOrderAsync(Cart current, BillingAddress billing, PaymentMethod paymentMethod, bool subscribeNewsletter, string newsletterSource)
    {
        var demand = current.Items
            .Where(i => i.Kind == CartItemKind.EventTicket)
            .Select(i => new TicketDemand(i.EventId, i.TierId, i.Quantity, i.IsConversion))
            .ToList();
        foreach (var eventId in current.Items.Where(i => i.Kind == CartItemKind.EventTicket).Select(i => i.EventId).Distinct())
        {
            if ((await admission.GetOccupancyAsync(eventId)).Full)
            {
                TempData["CartError"] = "Abendkasse geschlossen: Die Halle ist voll. Es können keine Tickets mehr gekauft werden.";
                return Redirect("/cart");
            }
        }

        foreach (var eventId in current.Items.Where(i => i.Kind == CartItemKind.EventTicket && !i.IsConversion).Select(i => i.EventId).Distinct())
        {
            if (await conversionRules.GetConversionOnlyAsync(eventId))
            {
                TempData["CartError"] = "Für einen Anlass im Warenkorb sind normale Ticketkäufe nicht möglich (nur Kartenumwandlung). Bitte das betroffene Ticket entfernen.";
                return Redirect("/cart");
            }
        }

        var capacityError = await pricing.CheckCapacityAsync(demand);
        capacityError ??= await CheckSeasonPassCapacityAsync(current);
        if (capacityError is not null)
        {
            TempData["CartError"] = capacityError;
            return Redirect("/cart");
        }

        var number = await orders.NextOrderNumberAsync();
        var order = Order.Create(number, billing, current.TotalAmount, VatRate, paymentMethod, sellerUid: null,
            paymentSource: PaymentSource.Online);
        order.SetFulfillmentPayload(BuildSnapshotJson(current, subscribeNewsletter, newsletterSource));
        var saved = await orders.SaveAsync(order);
        await orderLog.AppendAsync(saved.Id, OrderStatus.Draft, "Online-Kauf", "Bestellung erstellt");

        if (payrexx.Enabled && saved.TotalGross > 0m)
        {
            var baseUrl = publicUrl.Resolve();
            var request = new PayrexxCreateRequest(
                AmountInCents: (int)Math.Round(saved.TotalGross * 100m, MidpointRounding.AwayFromZero),
                Currency: saved.Currency,
                Purpose: $"Red Ants Ticketing {saved.OrderNumber}",
                ReferenceId: saved.OrderNumber,
                SuccessUrl: $"{baseUrl}/checkout/success?t={Uri.EscapeDataString(ProtectOrder(saved.Id))}",
                FailedUrl: $"{baseUrl}/checkout/cancel",
                CancelUrl: $"{baseUrl}/checkout/cancel",
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
                return Redirect(newsletterSource switch { "Express" => "/checkout/express", "QuickBuy" => "/next", _ => "/checkout" });
            }
        }

        var (issued, addOnInfos) = await FulfillAsync(saved.Id);
        cart.Clear();
        HttpContext.Session.Remove(FormKey);
        SaveConfirmation(new CheckoutConfirmationView
        {
            OrderNumber = saved.OrderNumber,
            Email = billing.Email,
            Total = saved.TotalGross,
            PaymentLabel = PaymentLabelText,
            Tickets = issued,
            AddOnInfoTexts = addOnInfos
        });
        return Redirect("/checkout/confirmation");
    }

    private static string BuildSnapshotJson(Cart cart, bool subscribeNewsletter, string newsletterSource)
    {
        var items = cart.Items
            .Select(i => new FulfillmentItem((int)i.Kind, i.EventId, i.SeasonId, i.TierId, i.UnitPrice, i.Quantity, i.EventName, i.CategoryName,
                i.OriginType, i.OriginCardUuid, i.OriginCategory))
            .ToList();
        var addOns = cart.Items
            .Where(i => i.Kind == CartItemKind.SeasonPass && i.AddOns.Count > 0)
            .SelectMany(i => i.AddOns.Select(a => new FulfillmentAddOn(a.Id, i.SeasonId, i.EventName, i.TierId, i.CategoryName, a.Label, a.Price, i.Quantity)))
            .Concat(cart.OrderAddOns.Select(a => new FulfillmentAddOn(a.Id, a.SeasonId, a.SeasonName, 0, "", a.Label, a.Price, 1)))
            .ToList();
        return JsonSerializer.Serialize(new FulfillmentSnapshot(items, addOns, subscribeNewsletter, newsletterSource));
    }

    private async Task<(List<ConfirmationTicket> Tickets, List<string> AddOnInfos)> FulfillAsync(int orderId)
    {
        var order = await orders.GetByIdAsync(orderId);
        if (order is null || order.Status != OrderStatus.Draft || string.IsNullOrEmpty(order.FulfillmentPayload))
            return ([], []);
        if (!await orders.TryMarkPaidAsync(orderId)) return ([], []);
        await orderLog.AppendAsync(order.Id, OrderStatus.Paid, "Online-Kauf", "Online bezahlt");

        var snapshot = JsonSerializer.Deserialize<FulfillmentSnapshot>(order.FulfillmentPayload);
        if (snapshot is null) return ([], []);
        var billing = order.BillingAddress;
        var buyer = billing.ToBuyer();
        var holderName = string.IsNullOrWhiteSpace(buyer.DisplayName) ? null : buyer.DisplayName;

        var issued = new List<ConfirmationTicket>();
        var mailTickets = new List<OrderMailTicket>();
        foreach (var item in snapshot.Items)
        {
            for (var i = 0; i < item.Quantity; i++)
            {
                if (item.Kind == (int)CartItemKind.SeasonPass)
                {
                    var pass = await passes.SaveAsync(
                        SeasonPass.Create(item.SeasonId, item.TierId, item.UnitPrice, order.Id, buyer, "Online-Kauf"));
                    var passToken = tokens.CreateShort(pass.Uuid);
                    var passCategory = await TicketCategoryNameAsync(pass.Uuid, item.CategoryName);
                    issued.Add(new ConfirmationTicket(pass.Uuid, item.EventName, passCategory, passToken, (int)TicketType.SeasonPass, await SeasonDateTextAsync(item.SeasonId), HolderName: holderName));
                    mailTickets.Add(new OrderMailTicket(
                        TicketType.SeasonPass, pass.Uuid, item.SeasonId, item.EventName, passCategory, holderName));
                    continue;
                }

                var originType = item.OriginType is { } ot ? (TicketType)ot : (TicketType?)null;
                var originUuid = Guid.TryParse(item.OriginCardUuid, out var ou) ? ou : (Guid?)null;
                var ticket = await tickets.SaveAsync(
                    EventTicket.Create(item.EventId, (TicketCategory)item.OriginCategory, item.UnitPrice, order.Id, buyer,
                        "Online-Kauf", tierId: item.TierId, originType: originType, originCardUuid: originUuid));
                if (originType == TicketType.SeasonSingle && originUuid is { } flexUuid)
                    await convertibleCards.MarkFlexConvertedAsync(flexUuid, item.EventId);
                var token = tokens.CreateShort(ticket.Uuid);
                var ticketCategory = await TicketCategoryNameAsync(ticket.Uuid, item.CategoryName);
                issued.Add(new ConfirmationTicket(ticket.Uuid, item.EventName, ticketCategory, token, (int)TicketType.EventTicket, await EventDateTextAsync(item.EventId), await EventVenueNameAsync(item.EventId), holderName));
                mailTickets.Add(new OrderMailTicket(
                    TicketType.EventTicket, ticket.Uuid, item.EventId, item.EventName, ticketCategory, holderName));
            }
        }

        await orders.CopyBillingToTicketsAsync(order.Id);

        if (snapshot.AddOns.Count > 0)
        {
            var addOnLines = snapshot.AddOns
                .Select(a => new OrderAddOnLine(a.SeasonId, a.EventName, default, a.CategoryName, a.Label, a.Price, a.Quantity, a.TierId))
                .ToList();
            await orderAddOns.SaveAsync(order.Id, addOnLines);
            await addOnNotifier.NotifyAsync(order.OrderNumber, billing.FullName, billing.Email, addOnLines);
        }

        var addOnInfos = await BuildAddOnInfosAsync(snapshot);

        await mailer.SendTicketsAsync(new OrderMailModel(
            order.OrderNumber, billing.Email, billing.FullName, order.TotalGross,
            publicUrl.Resolve(), mailTickets, addOnInfos));

        try
        {
            var orderItemLines = new List<OrderItem>();
            foreach (var item in snapshot.Items)
            {
                var kind = item.Kind == (int)CartItemKind.SeasonPass ? OrderItemKind.SeasonPass : OrderItemKind.EventTicket;
                var refId = item.Kind == (int)CartItemKind.SeasonPass ? item.SeasonId : item.EventId;
                var label = string.IsNullOrEmpty(item.CategoryName) ? item.EventName : $"{item.EventName} · {item.CategoryName}";
                orderItemLines.Add(OrderItem.Create(order.Id, kind, refId, default, label, item.Quantity, item.UnitPrice));
            }
            foreach (var a in snapshot.AddOns)
                orderItemLines.Add(OrderItem.Create(order.Id, OrderItemKind.AddOn, a.SeasonId, default, a.Label, a.Quantity, a.Price));
            if (orderItemLines.Count > 0)
                await orderItems.SaveAsync(order.Id, orderItemLines);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OrderItems für Bestellung {OrderNumber} konnten nicht gespeichert werden (per Backfill nachholbar)", order.OrderNumber);
        }

        if (snapshot.SubscribeNewsletter)
            await newsletter.SubscribeAsync(billing.Email, billing.FullName, snapshot.NewsletterSource);

        return (issued, addOnInfos);
    }

    [HttpGet("/checkout/confirmation")]
    public IActionResult Confirmation()
    {
        var json = HttpContext.Session.GetString(ConfirmationKey);
        if (string.IsNullOrEmpty(json)) return Redirect("/");
        var view = JsonSerializer.Deserialize<CheckoutConfirmationView>(json);
        return view is null ? Redirect("/") : View("~/Views/Checkout/Confirmation.cshtml", view);
    }

    [HttpGet("/checkout/success")]
    public async Task<IActionResult> Processing(string t)
    {
        if (UnprotectOrder(t) is not { } orderId) return Redirect("/");
        var found = await orders.GetByIdAsync(orderId);
        if (found is null) return Redirect("/");

        if (found.Status == OrderStatus.Draft && payrexx.Enabled && !string.IsNullOrEmpty(found.PayrexxGatewayId))
        {
            var status = await payrexx.GetGatewayStatusAsync(found.PayrexxGatewayId);
            if (status == PayrexxStatus.Confirmed)
            {
                await FulfillAsync(found.Id);
                found = await orders.GetByIdAsync(orderId) ?? found;
            }
            else if (status is PayrexxStatus.Cancelled or PayrexxStatus.Declined)
            {
                return Redirect("/checkout/cancel");
            }
        }

        var paid = found.Status == OrderStatus.Paid;
        if (paid && !IsQuickBuyOrder(found))
        {
            cart.Clear();
            HttpContext.Session.Remove(FormKey);
        }
        return View("~/Views/Checkout/Processing.cshtml", new CheckoutProcessingView
        {
            OrderId = found.Id,
            Token = ProtectOrder(found.Id),
            OrderNumber = found.OrderNumber,
            Email = found.BillingAddress.Email,
            AlreadyPaid = paid,
            Tickets = paid ? await BuildOrderTicketsAsync(found) : [],
            AddOnInfoTexts = paid ? await AddOnInfosForOrderAsync(found) : []
        });
    }

    private static bool IsQuickBuyOrder(Order order) =>
        !string.IsNullOrEmpty(order.FulfillmentPayload)
        && JsonSerializer.Deserialize<FulfillmentSnapshot>(order.FulfillmentPayload) is { NewsletterSource: "QuickBuy" };

    private async Task<string> TicketCategoryNameAsync(Guid uuid, string fallback)
    {
        var resolved = (await issuedTickets.FindAsync(uuid))?.CategoryName;
        return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved;
    }

    private async Task<List<ConfirmationTicket>> BuildOrderTicketsAsync(Order order)
    {
        var names = new Dictionary<(int Kind, int RefId, int TierId), (string EventName, string CategoryName)>();
        if (!string.IsNullOrEmpty(order.FulfillmentPayload)
            && JsonSerializer.Deserialize<FulfillmentSnapshot>(order.FulfillmentPayload) is { } snapshot)
        {
            foreach (var i in snapshot.Items)
            {
                var refId = i.Kind == (int)CartItemKind.SeasonPass ? i.SeasonId : i.EventId;
                names[(i.Kind, refId, i.TierId)] = (i.EventName, i.CategoryName);
            }
        }

        var holderName = string.IsNullOrWhiteSpace(order.BillingAddress.ToBuyer().DisplayName)
            ? null : order.BillingAddress.ToBuyer().DisplayName;

        var result = new List<ConfirmationTicket>();
        foreach (var t in await tickets.GetByOrderAsync(order.Id))
        {
            names.TryGetValue(((int)CartItemKind.EventTicket, t.EventId, t.TierId ?? 0), out var n);
            var token = tokens.CreateShort(t.Uuid);
            result.Add(new ConfirmationTicket(t.Uuid, n.EventName ?? "", await TicketCategoryNameAsync(t.Uuid, n.CategoryName ?? ""), token, (int)TicketType.EventTicket, await EventDateTextAsync(t.EventId), await EventVenueNameAsync(t.EventId), holderName));
        }
        foreach (var p in await passes.GetByOrderAsync(order.Id))
        {
            names.TryGetValue(((int)CartItemKind.SeasonPass, p.SeasonId, p.TierId ?? 0), out var n);
            var token = tokens.CreateShort(p.Uuid);
            result.Add(new ConfirmationTicket(p.Uuid, n.EventName ?? "", await TicketCategoryNameAsync(p.Uuid, n.CategoryName ?? ""), token, (int)TicketType.SeasonPass, await SeasonDateTextAsync(p.SeasonId), HolderName: holderName));
        }
        return result;
    }

    private async Task<IReadOnlyList<string>> AddOnInfosForOrderAsync(Order order) =>
        !string.IsNullOrEmpty(order.FulfillmentPayload)
        && JsonSerializer.Deserialize<FulfillmentSnapshot>(order.FulfillmentPayload) is { } snapshot
            ? await BuildAddOnInfosAsync(snapshot)
            : [];

    private async Task<List<string>> BuildAddOnInfosAsync(FulfillmentSnapshot snapshot)
    {
        var infos = new List<string>();
        foreach (var group in snapshot.AddOns.GroupBy(a => a.SeasonId))
        {
            var byId = (await seasonAddOns.GetBySeasonAsync(group.Key)).ToDictionary(a => a.Id);
            foreach (var a in group)
                if (byId.TryGetValue(a.Id, out var entity) && !string.IsNullOrWhiteSpace(entity.InfoAfterPurchase))
                    infos.Add(entity.InfoAfterPurchase!);
        }
        return infos.Distinct().ToList();
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

    [HttpGet("/checkout/status")]
    public async Task<IActionResult> Status(string t)
    {
        if (UnprotectOrder(t) is not { } orderId) return NotFound();
        var found = await orders.GetByIdAsync(orderId);
        if (found is null) return NotFound();

        var paid = found.Status == OrderStatus.Paid;
        var cancelled = found.Status is OrderStatus.Cancelled or OrderStatus.Refunded;

        if (!paid && !cancelled && found.Status == OrderStatus.Draft
            && payrexx.Enabled && !string.IsNullOrEmpty(found.PayrexxGatewayId))
        {
            try
            {
                var status = await payrexx.GetGatewayStatusAsync(found.PayrexxGatewayId);
                if (status == PayrexxStatus.Confirmed) paid = true;
                else if (status is PayrexxStatus.Cancelled or PayrexxStatus.Declined) cancelled = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Payrexx status check failed for order {Order}.", found.OrderNumber);
            }
        }

        return Json(new { paid, cancelled });
    }

    [HttpGet("/checkout/cancel")]
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

    private async Task<string?> EventDateTextAsync(int eventId) =>
        await events.FindByIdAsync(eventId) is { } ev
            ? ev.TimeUnknown ? $"{ev.Date:dd.MM.yyyy}" : $"{ev.Date:dd.MM.yyyy}, {ev.StartTime:HH:mm} Uhr"
            : null;

    private async Task<string?> EventVenueNameAsync(int eventId) =>
        await events.FindByIdAsync(eventId) is { VenueId: > 0 } ev
            ? (await venues.FindByIdAsync(ev.VenueId))?.Name
            : null;

    private async Task<string?> SeasonDateTextAsync(int seasonId) =>
        await seasons.FindByIdAsync(seasonId) is { } s
            ? $"{s.StartDate:dd.MM.yyyy} – {s.EndDate:dd.MM.yyyy}"
            : null;

    private void SaveForm(CheckoutForm form) =>
        HttpContext.Session.SetString(FormKey, JsonSerializer.Serialize(form));

    private void SaveConfirmation(CheckoutConfirmationView view) =>
        HttpContext.Session.SetString(ConfirmationKey, JsonSerializer.Serialize(view));
}
