namespace RedAnts.GameClock;

public sealed class ClockHub
{
    public ClockState Current { get; private set; } = ClockState.Empty;

    public event Action<ClockState>? Changed;

    public void Publish(ClockState state)
    {
        Current = state;
        Changed?.Invoke(state);
    }
}
