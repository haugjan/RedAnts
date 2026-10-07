using RedAnts.GameClock.Clocks;

namespace RedAnts.GameClock;

public sealed class ClockHub(ILogger<ClockHub> log)
{
    public ClockState Current { get; private set; } = ClockState.Empty;

    public event Action<ClockState>? Changed;

    public void Publish(ClockState state)
    {
        Current = state;
        foreach (var listener in Changed?.GetInvocationList() ?? [])
        {
            try { ((Action<ClockState>)listener)(state); }
            catch (Exception e) { log.LogDebug("Empfänger abgemeldet: {Message}", e.Message); }
        }
    }

    public void Reset()
    {
        Current = ClockState.Empty;
    }
}
