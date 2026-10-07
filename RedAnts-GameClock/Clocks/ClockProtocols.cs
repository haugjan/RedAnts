namespace RedAnts.GameClock.Clocks;

public sealed class ClockProtocols(IEnumerable<IClockProtocol> protocols)
{
    readonly IClockProtocol[] _all = protocols.ToArray();

    public IReadOnlyList<IClockProtocol> All => _all;

    public IClockProtocol Find(string key) =>
        _all.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
        ?? _all.First(p => p.Key == ICastProtocol.Id);

    public (IClockProtocol Protocol, int Score)? Best(byte[] payload) =>
        _all.Select(protocol => (Protocol: protocol, Score: protocol.Match(payload)))
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .Select(candidate => ((IClockProtocol, int)?)(candidate.Protocol, candidate.Score))
            .FirstOrDefault();
}
