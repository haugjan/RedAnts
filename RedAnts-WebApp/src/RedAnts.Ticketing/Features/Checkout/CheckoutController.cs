using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;
using System.Text.Json;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed class CheckoutController(
    ICartRepository carts,
    IOrders orders,
    IOrderTokens tokens,
    ICaptchaVerifier captcha,
    IPayrexxGateway payrexx,
    PlaceOrder.Handler placeOrder,
    ConfirmPayment.Handler confirmPayment,
    GetOrderConfirmation.Handler getConfirmation,
    GetCheckoutStatus.Handler getStatus,
    CancelDraftOrder.Handler cancelDraft,
    GetQuickBuyCart.Handler quickBuyCart,
    ILogger<CheckoutController> logger) : Controller
{
    private const string FormKey = "RedAnts.Checkout.Form";
    private const string ConfirmationKey = "RedAnts.Checkout.Confirmation";
    private const string PaymentLabelText = "Online-Zahlung (Payrexx)";
    private const string PrivacyError = "Bitte akzeptiere die AGB und die Datenschutzerklärung.";
    private const string CaptchaError = "Bitte bestätige, dass du kein Roboter bist.";
    private const string EmailError = "Bitte eine gültige E-Mail-Adresse angeben.";
    private const string MobileError = "Für die gewählte Zusatzoption ist deine Mobilnummer zwingend. Bitte gib sie an.";

    [HttpGet("/checkout")]
    public IActionResult Address(string? payment = null)
    {
        if (carts.Load().IsEmpty) return Redirect("/cart");
        var error = payment == "aborted"
            ? "Die Zahlung wurde abgebrochen oder ist fehlgeschlagen. Bitte versuche es erneut."
            : TempData["CheckoutError"] as string;
        return CheckoutView(LoadForm() ?? new CheckoutForm(), error);
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutForm form, bool acceptPrivacy)
    {
        var current = carts.Load();
        if (current.IsEmpty) return Redirect("/cart");

        SaveForm(form);

        BillingAddress billing;
        try { billing = ToBillingAddress(form); }
        catch (DomainException ex) { return CheckoutView(form, ex.Message); }

        if (!acceptPrivacy) return CheckoutView(form, PrivacyError);
        if (string.IsNullOrWhiteSpace(form.Phone) && current.RequiresMobileNumber) return CheckoutView(form, MobileError);
        if (!await CaptchaPassesAsync()) return CheckoutView(form, CaptchaError);

        var result = await placeOrder.HandleAsync(new PlaceOrder.Command(current, billing, form.AcceptNewsletter, CheckoutSource.Checkout));
        return await FinishAsync(result, message => CheckoutView(form, message));
    }

    [HttpGet("/checkout/payment")]
    public IActionResult Payment() => Redirect("/checkout");

    [HttpGet("/checkout/express")]
    public IActionResult Express()
    {
        var current = carts.Load();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!current.QualifiesForExpress) return Redirect("/checkout");
        return ExpressView(current, TempData["CheckoutError"] as string, "", "");
    }

    [HttpPost("/checkout/express")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressPay(string email, string? name, bool acceptNewsletter, bool acceptPrivacy)
    {
        var current = carts.Load();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!current.QualifiesForExpress) return Redirect("/checkout");

        email = (email ?? "").Trim();
        IActionResult Invalid(string error) => ExpressView(current, error, email, name ?? "");

        if (!LooksLikeEmail(email)) return Invalid(EmailError);
        if (!acceptPrivacy) return Invalid(PrivacyError);
        if (!await CaptchaPassesAsync()) return Invalid(CaptchaError);

        var result = await placeOrder.HandleAsync(new PlaceOrder.Command(current, GuestBilling(email, name), acceptNewsletter, CheckoutSource.Express));
        return await FinishAsync(result, Invalid);
    }

    [HttpPost("/next/buy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickBuy(int eventId, int tierId, string email, string? name, bool acceptNewsletter, bool acceptPrivacy)
    {
        email = (email ?? "").Trim();
        IActionResult Back(string error)
        {
            TempData["QuickError"] = error;
            TempData["QuickEmail"] = email;
            TempData["QuickName"] = name ?? "";
            return Redirect("/next");
        }

        if (!LooksLikeEmail(email)) return Back(EmailError);
        if (!acceptPrivacy) return Back(PrivacyError);
        if (!await CaptchaPassesAsync()) return Back(CaptchaError);

        var oneTicket = await quickBuyCart.HandleAsync(new GetQuickBuyCart.Query(eventId, tierId));
        if (oneTicket is null) return Back("Dieses Ticket ist nicht mehr verfügbar.");

        var result = await placeOrder.HandleAsync(new PlaceOrder.Command(oneTicket, GuestBilling(email, name), acceptNewsletter, CheckoutSource.QuickBuy));
        return await FinishAsync(result, Back);
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
        if (tokens.Unprotect(t) is not { } orderId) return Redirect("/");

        var payment = await confirmPayment.HandleAsync(new ConfirmPayment.Command(orderId));
        if (!payment.Found) return Redirect("/");
        if (payment.Cancelled) return Redirect("/checkout/cancel");

        var confirmation = await getConfirmation.HandleAsync(new GetOrderConfirmation.Query(orderId));
        if (confirmation is null) return Redirect("/");

        if (confirmation.Paid && !confirmation.IsQuickBuy)
        {
            carts.Clear();
            HttpContext.Session.Remove(FormKey);
        }

        return View("~/Views/Checkout/Processing.cshtml", new CheckoutProcessingView
        {
            OrderId = orderId,
            Token = tokens.Protect(orderId),
            OrderNumber = confirmation.OrderNumber,
            Email = confirmation.Email,
            AlreadyPaid = confirmation.Paid,
            Tickets = confirmation.Tickets,
            AddOnInfoTexts = confirmation.AddOnInfoTexts
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
        if (order is null) return Ok();

        try
        {
            await confirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));
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
        if (tokens.Unprotect(t) is not { } orderId) return NotFound();
        var status = await getStatus.HandleAsync(new GetCheckoutStatus.Query(orderId));
        if (!status.Found) return NotFound();
        return Json(new { paid = status.Paid, cancelled = status.Cancelled });
    }

    [HttpGet("/checkout/cancel")]
    public async Task<IActionResult> Cancelled(string? t = null)
    {
        if (tokens.Unprotect(t) is not { } orderId) return View("~/Views/Checkout/Cancelled.cshtml");

        var payment = await confirmPayment.HandleAsync(new ConfirmPayment.Command(orderId));
        if (payment.Paid) return Redirect($"/checkout/success?t={Uri.EscapeDataString(t!)}");
        if (payment.Found && !payment.Cancelled)
            await cancelDraft.HandleAsync(new CancelDraftOrder.Command(orderId, "Zahlung abgebrochen"));
        return View("~/Views/Checkout/Cancelled.cshtml");
    }

    private async Task<IActionResult> FinishAsync(PlaceOrder.Result result, Func<string, IActionResult> showError)
    {
        switch (result)
        {
            case PlaceOrder.Result.Denied { BackToCart: true } denied:
                TempData["CartError"] = denied.Message;
                return Redirect("/cart");
            case PlaceOrder.Result.Denied denied:
                return showError(denied.Message);
            case PlaceOrder.Result.PaymentRequired payment:
                return Redirect(payment.PaymentLink);
            case PlaceOrder.Result.Completed completed:
                return await CompletedAsync(completed.OrderId);
            default:
                throw new InvalidOperationException($"Unexpected checkout result {result.GetType().Name}.");
        }
    }

    private async Task<IActionResult> CompletedAsync(int orderId)
    {
        var confirmation = await getConfirmation.HandleAsync(new GetOrderConfirmation.Query(orderId));
        carts.Clear();
        HttpContext.Session.Remove(FormKey);
        if (confirmation is null) return Redirect("/");
        SaveConfirmation(new CheckoutConfirmationView
        {
            OrderNumber = confirmation.OrderNumber,
            Email = confirmation.Email,
            Total = confirmation.Total,
            PaymentLabel = PaymentLabelText,
            Tickets = confirmation.Tickets,
            AddOnInfoTexts = confirmation.AddOnInfoTexts
        });
        return Redirect("/checkout/confirmation");
    }

    private IActionResult CheckoutView(CheckoutForm form, string? error)
    {
        var current = carts.Load();
        return View("~/Views/Checkout/Address.cshtml", new CheckoutAddressView
        {
            Form = form,
            Cart = current,
            PayrexxEnabled = payrexx.Enabled,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = error,
            MobileRequired = current.RequiresMobileNumber
        });
    }

    private IActionResult ExpressView(Cart current, string? error, string email, string name) =>
        View("~/Views/Checkout/Express.cshtml", new CheckoutExpressView
        {
            Cart = current,
            PayrexxEnabled = payrexx.Enabled,
            TurnstileSiteKey = captcha.Enabled ? captcha.SiteKey : null,
            Error = error,
            Email = email,
            Name = name
        });

    private async Task<bool> CaptchaPassesAsync()
    {
        var token = Request.Form["cf-turnstile-response"].ToString();
        return await captcha.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString());
    }

    private static bool LooksLikeEmail(string email) =>
        email.Length >= 5 && email.Contains('@') && email.Contains('.');

    private static BillingAddress GuestBilling(string email, string? name)
    {
        var trimmed = (name ?? "").Trim();
        var space = trimmed.IndexOf(' ');
        var firstName = space > 0 ? trimmed[..space] : trimmed;
        var lastName = space > 0 ? trimmed[(space + 1)..] : "";
        return BillingAddress.FromPersistence((int)BuyerType.Private, firstName, lastName, null,
            "", null, "", "", "Schweiz", email, null);
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
