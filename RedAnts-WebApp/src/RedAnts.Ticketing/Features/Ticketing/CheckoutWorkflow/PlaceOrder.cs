using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Scanning;
using RedAnts.Features.Ticketing.Tickets;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

public static class PlaceOrder
{
    public sealed record Command(Cart Cart, BillingAddress Billing, bool SubscribeNewsletter, CheckoutSource Source);

    public abstract record Result
    {
        public sealed record Denied(string Message, bool BackToCart) : Result;

        public sealed record PaymentRequired(int OrderId, string PaymentLink) : Result;

        public sealed record Completed(int OrderId) : Result;
    }

    public sealed class Handler(
        IOrders orders,
        IOrderLog orderLog,
        IEventConversionRules conversionRules,
        IAdmissionService admission,
        ISeasonAddOns seasonAddOns,
        IPayrexxGateway payrexx,
        IPublicBaseUrl publicUrl,
        IOrderTokens tokens,
        CapacityReservation reservation,
        OrderFulfillment fulfillment,
        ILogger<Handler> logger)
    {
        private const decimal VatRate = 0m;

        public async Task<Result> HandleAsync(Command command)
        {
            var cart = command.Cart;
            if (cart.IsEmpty) return new Result.Denied("Der Warenkorb ist leer.", true);

            foreach (var eventId in cart.EventIds)
                if ((await admission.GetOccupancyAsync(eventId)).Full)
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
                var number = await orders.NextOrderNumberAsync();
                var order = Order.Create(number, command.Billing, cart.TotalAmount, VatRate, PaymentMethod.Payrexx, sellerUid: null,
                    paymentSource: PaymentSource.Online);
                order.SetFulfillmentPayload(JsonSerializer.Serialize(snapshot));
                saved = await orders.SaveAsync(order);
            }
            catch
            {
                await reservation.ReleaseAsync(snapshot);
                throw;
            }
            await orderLog.AppendAsync(saved.Id, OrderStatus.Draft, "Online-Kauf", "Bestellung erstellt");

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
                var definitions = await seasonAddOns.GetBySeasonAsync(group.Key);
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
                if (await orders.TryCancelDraftAsync(saved.Id))
                {
                    await reservation.ReleaseAsync(snapshot);
                    await orderLog.AppendAsync(saved.Id, OrderStatus.Cancelled, "System", "Zahlung konnte nicht gestartet werden");
                }
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
