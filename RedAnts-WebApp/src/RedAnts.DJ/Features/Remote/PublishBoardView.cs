namespace RedAnts.DJ.Features.Remote;

public static class PublishBoardView
{
    public sealed record Command(Guid BoardId, string? Room, DJBoardView View);

    public sealed class Handler(IDJBoardViews views)
    {
        public Task HandleAsync(Command command)
        {
            views.Publish(command.BoardId, command.Room, command.View);
            return Task.CompletedTask;
        }
    }
}
