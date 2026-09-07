using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SetSeasonPassHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(ISeasonPasses passes)
    {
        public Task HandleAsync(Command command) => passes.SetHolderAsync(command.Uuid, command.Holder);
    }
}
