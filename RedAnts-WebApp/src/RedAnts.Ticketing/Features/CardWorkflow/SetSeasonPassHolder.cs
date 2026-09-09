using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetSeasonPassHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(ISeasonPasses passes)
    {
        public Task HandleAsync(Command command) => passes.SetHolderAsync(command.Uuid, command.Holder);
    }
}
