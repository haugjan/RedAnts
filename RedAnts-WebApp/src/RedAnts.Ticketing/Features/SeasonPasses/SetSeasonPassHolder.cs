using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class SetSeasonPassHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(ISeasonPassRepository passes)
    {
        public Task HandleAsync(Command command) => passes.SetHolderAsync(command.Uuid, command.Holder);
    }
}
