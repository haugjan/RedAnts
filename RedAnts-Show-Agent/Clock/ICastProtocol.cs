namespace RedAnts.Show.Agent.Clock;

public static class ICastProtocol
{
    public const string Type = "iCast";
    public const int DefaultPort = 50085;

    const int MinimumFields = 13;
    const int ModeIndex = 12;

    public static bool Supports(string clockType) => clockType.Equals(Type, StringComparison.OrdinalIgnoreCase);

    public static ClockState? Parse(byte[] payload) => Parse(LineDecoder.Decode(payload));

    public static ClockState? Parse(string line)
    {
        var fields = line.Split(';').Select(x => x.Trim()).ToArray();
        if (fields.Length < MinimumFields) return null;

        return new ClockState(
            fields[0], fields[1], fields[2], fields[3],
            fields[10], fields[11], fields[ModeIndex],
            ClockState.Penalties(fields[4], fields[5]),
            ClockState.Penalties(fields[6], fields[7]),
            line, DateTime.Now);
    }
}
