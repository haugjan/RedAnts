using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Email;

public static class SendOrderMailSample
{
    public sealed record Command(string? To);

    public sealed record Result(string Html, bool? Sent);

    public sealed class Handler(IOrderMailer orderMailer, IPublicBaseUrl publicUrl)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var model = new OrderMailModel(
                "2026-000123", command.To ?? "max.muster@example.com", "Max Muster", 45m,
                publicUrl.Resolve(),
                [
                    new(TicketType.EventTicket, Guid.Empty, 0, "Red Ants vs. UHC Beispielgegner", "Erwachsen", "Max Muster"),
                    new(TicketType.SeasonPass, Guid.Empty, 0, "Saison 2026/27", "Erwachsen", "Max Muster"),
                    new(TicketType.MemberCard, Guid.Empty, 0, "Saison 2026/27", "Block 4", "Max Muster")
                ]);
            var sent = string.IsNullOrWhiteSpace(command.To) ? (bool?)null : await orderMailer.SendTicketsAsync(model);
            return new Result(await orderMailer.RenderAsync(model), sent);
        }
    }
}
