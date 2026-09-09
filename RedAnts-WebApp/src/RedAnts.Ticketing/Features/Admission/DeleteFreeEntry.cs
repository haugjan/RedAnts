namespace RedAnts.Ticketing.Features.Admission;

public static class DeleteFreeEntry
{
    public sealed record Command(Guid Uuid);

    public sealed class Handler(IFreeEntryRepository freeEntries)
    {
        public Task HandleAsync(Command command) => freeEntries.DeleteAsync(command.Uuid);
    }
}
