using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class ClockPortTopUpTests
{
    private static Hall With(string type, int port) =>
        Hall.Create("win4", "WIN4") with { Devices = new HallDevices(ClockType: type, ClockPort: port) };

    [Fact]
    public void An_icast_on_the_wrong_port_is_moved_to_the_one_the_clock_broadcasts_on()
    {
        var filled = ClockPortTopUp.Fill(With("iCast", 50086));

        Assert.Equal(50085, filled!.Devices.ClockPort);
    }

    [Fact]
    public void A_hall_that_is_already_right_is_left_alone() =>
        Assert.Null(ClockPortTopUp.Fill(With("iCast", 50085)));

    [Fact]
    public void A_port_somebody_chose_on_purpose_is_left_alone() =>
        Assert.Null(ClockPortTopUp.Fill(With("iCast", 51000)));

    [Fact]
    public void A_bodet_keeps_its_own_port() =>
        Assert.Null(ClockPortTopUp.Fill(With("Bodet", 50086)));

    [Fact]
    public void The_shipped_default_is_the_port_the_clock_uses() =>
        Assert.Equal(ClockPortTopUp.Right, new HallDevices().ClockPort);
}
