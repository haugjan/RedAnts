using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.Tickets;
using System.Text.Json;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Features.Checkout;

public static class PlaceOrder
{
    public sealed record Command(Cart? Cart, BillingAddress Billing, bool SubscribeNewsletter, CheckoutSource Source)
    {
        public static Command FromSessionCart(BillingAddress billing, bool subscribeNewsletter, CheckoutSource source) =>
            new(null, billing, subscribeNewsletter, source);
    }

    public abstract record Result
    {
        public sealed record Denied(string Message, bool BackToCart) : Result;

        public sealed record PaymentRequired(int OrderId, string PaymentLink) : Result;

        public sealed record Completed(int OrderId) : Result;
    }

    public sealed class Handler(
        ICartRepository carts,
        IOrderRepository orders,
        IOrderLog orderLog,
        IEventConversionRuleRepository conversionRules,
        IOccupancyReader occupancy,
        ISeasonAddOnRepository seasonAddOns,
        IPayrexxGateway payrexx,
        IPublicBaseUrl publicUrl,
        IOrderTokens tokens,
        IUnitOfWork unitOfWork,
        CapacityReservation reservation,
        OrderFulfillment fulfillment,
        ILogger<Handler> logger)
    {
        private const decimal VatRate = 0m;

        public async Task<Result> HandleAsync(Command command)
        {
            var cart = command.Cart ?? carts.Load();
            if (cart.IsEmpty) return new Result.Denied("Der Warenkorb ist leer.", true);

            foreach (var eventId in cart.EventIds)
                if ((await occupancy.GetAsync(eventId)).Full)
                    return new Result.Denied("Abendkasse geschlossen: Die Halle ist voll. Es können keine Tickets mehr gekauft werden.", true);

            foreach (var eventId in cart.EventIds.Where(cart.HasRegularTicketsFor))
                if (await conversionRules.GetConversionOnlyAsync(eventId))
                    return new Result.Denied("Für einen Anlass im Warenkorb sind normale Ticketkäufe nicht möglich (nur Kartenumwandlung). Bitte das betroffene Ticket entfernen.", true);

            if (string.IsNullOrWhiteSpace(command.Billing.Phone) && await RequiresMobileNumberAsync(cart))
                return new Result.Denied("Für die gewählte Zusatzoption ist deine Mobilnummer zwingend. Bitte gib sie an.", false);

            var snapshot = OrderSnapshot.FromCart(cart, command.SubscribeNewsletter, command.Source);
            if (await reservation.TryReserveAsync(snapshot) is CheckResult.Denied denied)
                return new Result.Denied(DeniedMessage(denied.Cause, cart), true);

            Order saved;
            try
            {
                saved = await unitOfWork.RunAsync(async () =>
                {
                    var number = await orders.NextOrderNumberAsync();
                    var order = Order.Create(number, command.Billing, cart.TotalAmount, VatRate, PaymentMethod.Payrexx, sellerUid: null,
                        paymentSource: PaymentSource.Online);
                    order.SetFulfillmentPayload(JsonSerializer.Serialize(snapshot));
                    var stored = await orders.SaveAsync(order);
                    await orderLog.AppendAsync(stored.Id, OrderStatus.Draft, "Online-Kauf", "Bestellung erstellt");
                    return stored;
                });
            }
            catch
            {
                await reservation.ReleaseAsync(snapshot);
                throw;
            }

            if (payrexx.Enabled && saved.TotalGross > 0m)
                return await StartPaymentAsync(saved, command.Billing, snapshot);

            await fulfillment.FulfillAsync(saved.Id);
            return new Result.Completed(saved.Id);
        }

        private async Task<bool> RequiresMobileNumberAsync(Cart cart)
        {
            if (cart.RequiresMobileNumber) return true;

            var addOnIdsBySeason = cart.Items
                .Where(i => i.Kind == CartLineKind.SeasonPass)
                .SelectMany(i => i.AddOns.Select(a => (SeasonId: i.SeasonId, AddOnId: a.Id)))
                .Concat(cart.OrderAddOns.Select(a => (SeasonId: a.SeasonId, AddOnId: a.Id)))
                .GroupBy(x => x.SeasonId, x => x.AddOnId);

            foreach (var group in addOnIdsBySeason)
            {
                var ids = group.ToHashSet();
                var definitions = (await seasonAddOns.LoadSeasonAsync(group.Key)).AddOns;
                if (definitions.Any(d => ids.Contains(d.Id) && d.RequireMobileNumber)) return true;
            }
            return false;
        }

        private async Task<Result> StartPaymentAsync(Order saved, BillingAddress billing, OrderSnapshot snapshot)
        {
            var baseUrl = publicUrl.Resolve();
            var token = Uri.EscapeDataString(tokens.Protect(saved.Id));
            var request = new PayrexxCreateRequest(
                AmountInCents: (int)Math.Round(saved.TotalGross * 100m, MidpointRounding.AwayFromZero),
                Currency: saved.Currency,
                Purpose: $"Red Ants Ticketing {saved.OrderNumber}",
                ReferenceId: saved.OrderNumber,
                SuccessUrl: $"{baseUrl}/checkout/success?t={token}",
                FailedUrl: $"{baseUrl}/checkout/cancel?t={token}",
                CancelUrl: $"{baseUrl}/checkout/cancel?t={token}",
                Email: billing.Email,
                FirstName: billing.FirstName,
                LastName: billing.LastName);
            try
            {
                var gateway = await payrexx.CreateGatewayAsync(request);
                saved.SetPayrexxGatewayId(gateway.GatewayId);
                await orders.SaveAsync(saved);
                return new Result.PaymentRequired(saved.Id, gateway.Link);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payrexx gateway creation failed for order {Order}.", saved.OrderNumber);
                var cancelled = await unitOfWork.RunAsync(async () =>
                {
                    if (!await orders.TryCancelDraftAsync(saved.Id)) return false;
                    await orderLog.AppendAsync(saved.Id, OrderStatus.Cancelled, "System", "Zahlung konnte nicht gestartet werden");
                    return true;
                });
                if (cancelled) await reservation.ReleaseAsync(snapshot);
                return new Result.Denied("Die Zahlung konnte nicht gestartet werden. Bitte versuche es erneut.", false);
            }
        }

        private static string DeniedMessage(CheckResult.Denied.Reason reason, Cart cart) => reason switch
        {
            CapacityDenied.TotalExhausted => "Für einen Anlass im Warenkorb sind nicht mehr genügend Tickets verfügbar.",
            CapacityDenied.TierUnavailable unavailable => $"«{TierName(cart, unavailable.TierId)}» ist nicht mehr verfügbar.",
            CapacityDenied.TierExhausted exhausted => $"«{TierName(cart, exhausted.TierId)}» ist nicht mehr in dieser Anzahl verfügbar (noch {exhausted.Remaining}).",
            _ => reason.Message
        };

        private static string TierName(Cart cart, int tierId) =>
            cart.Items.FirstOrDefault(i => i.TierId == tierId)?.CategoryName is { Length: > 0 } name ? name : "Die gewählte Preisstufe";
    }
}
