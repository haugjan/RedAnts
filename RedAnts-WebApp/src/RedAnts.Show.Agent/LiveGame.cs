using RedAnts.Show.Agent.Clock;

namespace RedAnts.Show.Agent;

public sealed class LiveGame
{
    static readonly TimeSpan ClockTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan ShortestStill = TimeSpan.FromMilliseconds(220);
    static readonly TimeSpan LongestStill = TimeSpan.FromMilliseconds(1400);
    static readonly TimeSpan Margin = TimeSpan.FromMilliseconds(120);

    readonly object _gate = new();
    ClockState _clock = ClockState.Empty;
    string _lastTime = "";
    DateTime _timeMovedAt = DateTime.MinValue;
    TimeSpan _tick = TimeSpan.FromSeconds(1);
    int _manualHome, _manualAway;
    DateTime _homeScoredAt = DateTime.MinValue;
    DateTime _awayScoredAt = DateTime.MinValue;
    TaskCompletionSource<bool> _scoreChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public event Action? Changed;

    public ClockState Clock
    {
        get { lock (_gate) return _clock; }
    }

    public bool ClockLive
    {
        get
        {
            var clock = Clock;
            return clock.HasData && DateTime.Now - clock.Received < ClockTimeout;
        }
    }

    public bool ClockRunning
    {
        get
        {
            lock (_gate) return ClockLive && DateTime.Now - _timeMovedAt < Still;
        }
    }

    TimeSpan Still
    {
        get
        {
            var window = _tick + Margin;
            return window < ShortestStill ? ShortestStill : window > LongestStill ? LongestStill : window;
        }
    }

    public string HomeScore => ClockLive ? Clock.HomeScore : ManualHome.ToString();

    public string AwayScore => ClockLive ? Clock.GuestScore : ManualAway.ToString();

    public string Period => ClockLive ? Clock.Period : "";

    public string PeriodLabel => ClockLive ? Clock.PeriodLabel : "";

    public string Time => ClockLive ? Clock.Time : "--:--";

    public int ManualHome { get { lock (_gate) return _manualHome; } }

    public int ManualAway { get { lock (_gate) return _manualAway; } }

    public void Update(ClockState state)
    {
        bool scoreChanged;
        lock (_gate)
        {
            scoreChanged = state.HomeScore != _clock.HomeScore || state.GuestScore != _clock.GuestScore;
            if (_clock.HasData && Rose(_clock.HomeScore, state.HomeScore)) _homeScoredAt = DateTime.Now;
            if (_clock.HasData && Rose(_clock.GuestScore, state.GuestScore)) _awayScoredAt = DateTime.Now;
            if (state.Time != _lastTime)
            {
                var moved = DateTime.Now;
                var gap = moved - _timeMovedAt;
                if (_timeMovedAt != DateTime.MinValue && gap > TimeSpan.Zero && gap < LongestStill * 2) _tick = gap;
                _lastTime = state.Time;
                _timeMovedAt = moved;
            }
            _clock = state;
            if (int.TryParse(state.HomeScore, out var home)) _manualHome = home;
            if (int.TryParse(state.GuestScore, out var away)) _manualAway = away;
        }
        if (scoreChanged) SignalScore();
        Changed?.Invoke();
    }

    public void AdjustScore(string side, int delta)
    {
        lock (_gate)
        {
            if (side.Equals("away", StringComparison.OrdinalIgnoreCase))
            {
                _manualAway = Math.Max(0, _manualAway + delta);
                if (delta > 0) _awayScoredAt = DateTime.Now;
            }
            else
            {
                _manualHome = Math.Max(0, _manualHome + delta);
                if (delta > 0) _homeScoredAt = DateTime.Now;
            }
        }
        SignalScore();
        Changed?.Invoke();
    }

    public async Task<bool> WaitForScoreChangeAsync(string home, string away, TimeSpan timeout, CancellationToken stop)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            Task signal;
            lock (_gate)
            {
                if (HomeScore != home || AwayScore != away) return true;
                signal = _scoreChanged.Task;
            }
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) return false;
            var finished = await Task.WhenAny(signal, Task.Delay(remaining, stop));
            stop.ThrowIfCancellationRequested();
            if (finished != signal) return HomeScore != home || AwayScore != away;
        }
    }

    public string GoalScore(string side, TimeSpan window)
    {
        var away = side.Equals("away", StringComparison.OrdinalIgnoreCase);
        var home = Number(HomeScore);
        var guest = Number(AwayScore);
        if (!Counted(away, window))
        {
            if (away) guest++;
            else home++;
        }
        return $"{home}:{guest}";
    }

    bool Counted(bool away, TimeSpan window)
    {
        lock (_gate) return DateTime.Now - (away ? _awayScoredAt : _homeScoredAt) < window;
    }

    static bool Rose(string before, string after) =>
        int.TryParse(after, out var now) && (!int.TryParse(before, out var was) || now > was);

    static int Number(string value) => int.TryParse(value, out var parsed) ? parsed : 0;

    void SignalScore()
    {
        TaskCompletionSource<bool> waiters;
        lock (_gate)
        {
            waiters = _scoreChanged;
            _scoreChanged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        waiters.TrySetResult(true);
    }
}
