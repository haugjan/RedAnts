using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public static class SetEventConversionRules
{
    public sealed record Command(int EventId, bool SeasonPassRequired, bool MemberCardRequired, decimal? FlexDiscount, bool ConversionOnly);

    public sealed class Handler(IEventConversionRuleRepository rules)
    {
        public async Task HandleAsync(Command command)
        {
            var id = command.EventId;
            await rules.SetAsync(id, TicketType.SeasonPass, command.SeasonPassRequired ? 0m : null);
            await rules.SetAsync(id, TicketType.MemberCard, command.MemberCardRequired ? 0m : null);
            await rules.SetAsync(id, TicketType.SeasonSingle,
                command.FlexDiscount is { } discount ? Math.Max(0m, decimal.Round(discount, 2)) : null);
            await rules.SetConversionOnlyAsync(id, command.ConversionOnly);
        }
    }
}
