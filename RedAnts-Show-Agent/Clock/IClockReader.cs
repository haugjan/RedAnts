namespace RedAnts.Show.Agent.Clock;

public enum ClockTransport { Udp, Tcp }

public interface IClockReader
{
    ClockState? Read(byte[] payload);

    string Describe(byte[] payload);
}

public sealed class ICastReader : IClockReader
{
    public ClockState? Read(byte[] payload) => ICastProtocol.Parse(Describe(payload));

    public string Describe(byte[] payload) => LineDecoder.Decode(payload);
}

public static class ClockTypes
{
    public static bool Supported(string? clockType) =>
        ICastProtocol.Supports(clockType) || BodetProtocol.Supports(clockType);

    public static string Name(string? clockType) =>
        BodetProtocol.Supports(clockType) ? BodetProtocol.Type : ICastProtocol.Type;

    public static ClockTransport Transport(string? clockType) =>
        BodetProtocol.Supports(clockType) ? ClockTransport.Tcp : ClockTransport.Udp;

    public static int DefaultPort(string? clockType) =>
        BodetProtocol.Supports(clockType) ? BodetProtocol.DefaultPort : ICastProtocol.DefaultPort;

    public static IClockReader Reader(string? clockType) =>
        BodetProtocol.Supports(clockType) ? new BodetReader() : new ICastReader();
}
