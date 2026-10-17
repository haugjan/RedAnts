namespace RedAnts.Show.Simulator.Simulation;

public sealed record LogEntry(DateTime At, string Source, string Text);

public sealed class EventLog
{
    private const int Capacity = 400;
    private readonly LinkedList<LogEntry> _entries = new();
    private readonly object _gate = new();

    public event Action? Changed;

    public void Add(string source, string text)
    {
        lock (_gate)
        {
            _entries.AddFirst(new LogEntry(DateTime.Now, source, text));
            while (_entries.Count > Capacity) _entries.RemoveLast();
        }
        Changed?.Invoke();
    }

    public IReadOnlyList<LogEntry> Latest(int count = 60)
    {
        lock (_gate) return _entries.Take(count).ToList();
    }

    public void Clear()
    {
        lock (_gate) _entries.Clear();
        Changed?.Invoke();
    }
}
