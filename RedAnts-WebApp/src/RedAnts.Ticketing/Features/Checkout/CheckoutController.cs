using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using System.Text.Json;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed class CheckoutController(
    GetCart.Handler getCart,
    GetCheckoutSettings.Handler getSettings,
    VerifyCaptcha.Handler verifyCaptcha,
    FindCheckoutOrder.Handler findOrder,
    PlaceOrder.Handler placeOrder,
    ConfirmPayment.Handler confirmPayment,
    GetOrderConfirmation.Handler getConfirmation,
    GetCheckoutStatus.Handler getStatus,
    CancelDraftOrder.Handler cancelDraft,
    GetQuickBuyCart.Handler quickBuyCart,
    ClearCart.Handler clearCart,
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
    public async Task<IActionResult> Address(string? payment = null)
    {
        var current = await CartAsync();
        if (current.IsEmpty) return Redirect("/cart");
        var error = payment == "aborted"
            ? "Die Zahlung wurde abgebrochen oder ist fehlgeschlagen. Bitte versuche es erneut."
            : TempData["CheckoutError"] as string;
        return await CheckoutViewAsync(LoadForm() ?? new CheckoutForm(), error, current);
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutForm form, bool acceptPrivacy)
    {
        var current = await CartAsync();
        if (current.IsEmpty) return Redirect("/cart");

        SaveForm(form);

        BillingAddress billing;
        try { billing = ToBillingAddress(form); }
        catch (DomainException ex) { return await CheckoutViewAsync(form, ex.Message, current); }

        if (!acceptPrivacy) return await CheckoutViewAsync(form, PrivacyError, current);
        if (string.IsNullOrWhiteSpace(form.Phone) && current.RequiresMobileNumber) return await CheckoutViewAsync(form, MobileError, current);
        if (!await CaptchaPassesAsync()) return await CheckoutViewAsync(form, CaptchaError, current);

        var result = await placeOrder.HandleAsync(PlaceOrder.Command.FromSessionCart(billing, form.AcceptNewsletter, CheckoutSource.Checkout));
        return await FinishAsync(result, message => CheckoutViewAsync(form, message, current));
    }

    [HttpGet("/checkout/payment")]
    public IActionResult Payment() => Redirect("/checkout");

    [HttpGet("/checkout/express")]
    public async Task<IActionResult> Express()
    {
        var current = await CartAsync();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!current.QualifiesForExpress) return Redirect("/checkout");
        return await ExpressViewAsync(current, TempData["CheckoutError"] as string, "", "");
    }

    [HttpPost("/checkout/express")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressPay(string email, string? name, bool acceptNewsletter, bool acceptPrivacy)
    {
        var current = await CartAsync();
        if (current.IsEmpty) return Redirect("/ticketing/");
        if (!current.QualifiesForExpress) return Redirect("/checkout");

        email = (email ?? "").Trim();
        Task<IActionResult> Invalid(string error) => ExpressViewAsync(current, error, email, name ?? "");

        if (!LooksLikeEmail(email)) return await Invalid(EmailError);
        if (!acceptPrivacy) return await Invalid(PrivacyError);
        if (!await CaptchaPassesAsync()) return await Invalid(CaptchaError);

        var result = await placeOrder.HandleAsync(PlaceOrder.Command.FromSessionCart(GuestBilling(email, name), acceptNewsletter, CheckoutSource.Express));
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
        return await FinishAsync(result, error => Task.FromResult(Back(error)));
    }

    [HttpGet("/checkout/confirmation")]
    public IActionResult Confirmation()
    {
        var json = HttpContext.Session.GetString(ConfirmationKey);
        if (string.IsNullOrEmpty(json)) return Redirect("/");
        var view = JsonSerializer.Deserialize<CheckoutConfirmationView>(json);
        return view is null ? Redirect("/") : View("Confirmation", view);
    }

    [HttpGet("/checkout/success")]
    public async Task<IActionResult> Processing(string t)
    {
        if (await findOrder.HandleAsync(new FindCheckoutOrder.Query(Token: t)) is not { } orderId) return Redirect("/");

        var payment = await confirmPayment.HandleAsync(new ConfirmPayment.Command(orderId));
        if (!payment.Found) return Redirect("/");
        if (payment.Cancelled) return Redirect("/checkout/cancel");

        var confirmation = await getConfirmation.HandleAsync(new GetOrderConfirmation.Query(orderId));
        if (confirmation is null) return Redirect("/");

        if (confirmation.Paid && !confirmation.IsQuickBuy)
        {
            await clearCart.HandleAsync(new ClearCart.Command());
            HttpContext.Session.Remove(FormKey);
        }

        return View("Processing", new CheckoutProcessingView
        {
            OrderId = orderId,
            Token = t,
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

        if (await findOrder.HandleAsync(new FindCheckoutOrder.Query(OrderNumber: reference)) is not { } orderId) return Ok();

        try
        {
            await confirmPayment.HandleAsync(new ConfirmPayment.Command(orderId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payrexx webhook processing failed for order {Order}.", reference.Trim());
        }
        return Ok();
    }

    [HttpGet("/checkout/status")]
    public async Task<IActionResult> Status(string t)
    {
        if (await findOrder.HandleAsync(new FindCheckoutOrder.Query(Token: t)) is not { } orderId) return NotFound();
        var status = await getStatus.HandleAsync(new GetCheckoutStatus.Query(orderId));
        if (!status.Found) return NotFound();
        return Json(new { paid = status.Paid, cancelled = status.Cancelled });
    }

    [HttpGet("/checkout/cancel")]
    public async Task<IActionResult> Cancelled(string? t = null)
    {
        if (await findOrder.HandleAsync(new FindCheckoutOrder.Query(Token: t)) is not { } orderId) return View("Cancelled");

        var payment = await confirmPayment.HandleAsync(new ConfirmPayment.Command(orderId));
        if (payment.Paid) return Redirect($"/checkout/success?t={Uri.EscapeDataString(t!)}");
        if (payment.Found && !payment.Cancelled)
            await cancelDraft.HandleAsync(new CancelDraftOrder.Command(orderId, "Zahlung abgebrochen"));
        return View("Cancelled");
    }

    private async Task<IActionResult> FinishAsync(PlaceOrder.Result result, Func<string, Task<IActionResult>> showError)
    {
        switch (result)
        {
            case PlaceOrder.Result.Denied { BackToCart: true } denied:
                TempData["CartError"] = denied.Message;
                return Redirect("/cart");
            case PlaceOrder.Result.Denied denied:
                return await showError(denied.Message);
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
        await clearCart.HandleAsync(new ClearCart.Command());
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

    private Task<CartSummary> CartAsync() => getCart.HandleAsync(new GetCart.Query());

    private async Task<IActionResult> CheckoutViewAsync(CheckoutForm form, string? error, CartSummary current)
    {
        var settings = await getSettings.HandleAsync(new GetCheckoutSettings.Query());
        return View("Address", new CheckoutAddressView
        {
            Form = form,
            Cart = current,
            PayrexxEnabled = settings.PayrexxEnabled,
            TurnstileSiteKey = settings.TurnstileSiteKey,
            Error = error,
            MobileRequired = current.RequiresMobileNumber
        });
    }

    private async Task<IActionResult> ExpressViewAsync(CartSummary current, string? error, string email, string name)
    {
        var settings = await getSettings.HandleAsync(new GetCheckoutSettings.Query());
        return View("Express", new CheckoutExpressView
        {
            Cart = current,
            PayrexxEnabled = settings.PayrexxEnabled,
            TurnstileSiteKey = settings.TurnstileSiteKey,
            Error = error,
            Email = email,
            Name = name
        });
    }

    private Task<bool> CaptchaPassesAsync() =>
        verifyCaptcha.HandleAsync(new VerifyCaptcha.Query(
            Request.Form["cf-turnstile-response"].ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString()));

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
