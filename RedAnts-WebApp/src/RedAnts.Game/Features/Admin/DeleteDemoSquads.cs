namespace RedAnts.Game.Features.Admin;

public static class DeleteDemoSquads
{
    public sealed record Command;

    public sealed class Handler(IDemoSquads demo)
    {
        public Task<int> HandleAsync(Command command) => demo.DeleteAsync();
    }
}
