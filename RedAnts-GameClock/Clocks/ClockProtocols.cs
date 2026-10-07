namespace RedAnts.GameClock.Clocks;

public sealed class ClockProtocols(IEnumerable<IClockProtocol> protocols)
{
    readonly IClockProtocol[] _all = protocols.ToArray();

    public IReadOnlyList<IClockProtocol> All => _all;

    public IClockProtocol Find(string key) =>
        _all.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
        ?? _all.First(p => p.Key == ICastProtocol.Id);

    public IEnumerable<IClockProtocol> Ranked(string line) =>
        _all.Select(p => (Protocol: p, Score: p.Match(line)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Protocol);

    public (IClockProtocol Protocol, int Score)? Best(string line) =>
        _all.Select(p => (Protocol: p, Score: p.Match(line)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => ((IClockProtocol, int)?)(x.Protocol, x.Score))
            .FirstOrDefault();
}
