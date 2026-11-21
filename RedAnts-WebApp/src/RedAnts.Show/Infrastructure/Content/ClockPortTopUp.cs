using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class ClockPortTopUp
{
    public const string SettingKey = "Show:ClockPort";
    public const string Version = "1-icast-50085";

    public const int Wrong = 50086;
    public const int Right = 50085;

    public static Hall? Fill(Hall hall) =>
        hall.Devices.ClockType.Equals("iCast", StringComparison.OrdinalIgnoreCase) && hall.Devices.ClockPort == Wrong
            ? hall with { Devices = hall.Devices with { ClockPort = Right } }
            : null;
}
