using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.TabBoards;

public static class SaveTabBoard
{
    public sealed record Command(TabBoard Board);

    public sealed class Handler(ITabBoardRepository boards)
    {
        public async Task<TabBoard> HandleAsync(Command command)
        {
            var board = command.Board.Validated();
            await boards.SaveAsync(board);
            return board;
        }
    }
}
