namespace RedAnts.Show.Features.Remote;

public static class PublishBoardView
{
    public sealed record Command(Guid BoardId, string? Room, ShowBoardView View);

    public sealed class Handler(IShowBoardViews views)
    {
        public Task HandleAsync(Command command)
        {
            views.Publish(command.BoardId, command.Room, command.View);
            return Task.CompletedTask;
        }
    }
}
