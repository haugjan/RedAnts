namespace RedAnts.Show.Features.Halls;

public static class DeleteHall
{
    public sealed record Command(string HallId);

    public sealed class Handler(IHallRepository halls)
    {
        public Task HandleAsync(Command command) => halls.DeleteAsync(command.HallId);
    }
}
