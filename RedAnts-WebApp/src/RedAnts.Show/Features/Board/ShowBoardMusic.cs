using System.Collections.Concurrent;

namespace RedAnts.Show.Features.Board;

public sealed record BoardPlayback(string Player, string MusicId, long MusicOrder, string Control, long ControlOrder)
{
    public static readonly BoardPlayback Idle = new("", "", 0, "", 0);

    public bool PlayedBy(string board) => Player.Length == 0 || Player == board;
}

public sealed class ShowBoardMusic
{
    private readonly ConcurrentDictionary<string, BoardPlayback> _halls = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string>? Changed;

    public BoardPlayback Of(string hallId) =>
        _halls.TryGetValue(hallId, out var state) ? state : BoardPlayback.Idle;

    public void Claim(string hallId, string board)
    {
        var state = Of(hallId);
        if (state.Player == board) return;
        Write(hallId, state with { Player = board });
    }

    public void Play(string hallId, string board, string musicId)
    {
        var state = Of(hallId);
        Write(hallId, state with { Player = board, MusicId = musicId, MusicOrder = state.MusicOrder + 1 });
    }

    public void Command(string hallId, string board, string what)
    {
        var state = Of(hallId);
        var stopped = what == "stop";
        Write(hallId, state with
        {
            Player = board,
            MusicId = stopped || what == "fade" ? "" : state.MusicId,
            MusicOrder = stopped ? state.MusicOrder + 1 : state.MusicOrder,
            Control = what,
            ControlOrder = state.ControlOrder + 1,
        });
    }

    private void Write(string hallId, BoardPlayback state)
    {
        _halls[hallId] = state;
        Changed?.Invoke(hallId);
    }
}
