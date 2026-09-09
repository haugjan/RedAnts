using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class CreateSeasonPass
{
    public sealed record Command(int SeasonId, int? TierId, decimal Price, int OrderId, Buyer Buyer, string? Reference, string? Email,
        CardHolder Holder, string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(ISeasonPasses passes)
    {
        public async Task<SeasonPass> HandleAsync(Command command)
        {
            var saved = await passes.SaveAsync(SeasonPass.Create(command.SeasonId, command.TierId, command.Price, command.OrderId,
                command.Buyer, command.CreatedByName, command.CreatedByEmail, command.Reference, command.Email));
            await passes.SetHolderAsync(saved.Uuid, command.Holder);
            return saved;
        }
    }
}
