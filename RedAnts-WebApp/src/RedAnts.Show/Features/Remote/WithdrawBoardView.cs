namespace RedAnts.Show.Features.Remote;

public static class WithdrawBoardView
{
    public sealed record Command(Guid BoardId);

    public sealed class Handler(IShowBoardViews views)
    {
        public Task HandleAsync(Command command)
        {
            views.Withdraw(command.BoardId);
            return Task.CompletedTask;
        }
    }
}
