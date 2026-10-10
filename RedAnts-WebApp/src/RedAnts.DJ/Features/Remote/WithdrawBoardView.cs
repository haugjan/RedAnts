namespace RedAnts.DJ.Features.Remote;

public static class WithdrawBoardView
{
    public sealed record Command(Guid BoardId);

    public sealed class Handler(IDJBoardViews views)
    {
        public Task HandleAsync(Command command)
        {
            views.Withdraw(command.BoardId);
            return Task.CompletedTask;
        }
    }
}
