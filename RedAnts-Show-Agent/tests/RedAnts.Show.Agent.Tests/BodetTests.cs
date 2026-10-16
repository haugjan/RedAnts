using RedAnts.Show.Agent.Clock;

namespace RedAnts.Show.Agent.Tests;

public class BodetTests
{
    const byte Floorball = (byte)'7';

    static readonly byte[] Documented =
    [
        0x01, 0x7f, 0x02, 0x47, 0x31, 0x31, 0x80, 0x37, 0x20, 0x34, 0x30,
        0x37, 0x20, 0x30, 0x31, 0x20, 0x30, 0x30, 0x31, 0x03, 0x2d,
    ];

    [Fact]
    public void Reads_the_documented_frame()
    {
        var state = new BodetReader().Read(Documented);

        Assert.NotNull(state);
        Assert.Equal("04:07", state.Time);
        Assert.Equal("1", state.HomeScore);
        Assert.Equal("0", state.GuestScore);
        Assert.Equal("1", state.Period);
        Assert.Equal("1. DRITTEL", state.PeriodLabel);
    }

    [Fact]
    public void Computes_the_lrc_the_way_the_manual_describes_it() =>
        Assert.Equal(0x2d, BodetFrame.Lrc(Documented, Documented.Length - 1));

    [Fact]
    public void Drops_a_frame_whose_lrc_does_not_match()
    {
        var broken = Documented.ToArray();
        broken[^1] ^= 0x01;

        Assert.False(BodetFrame.IsValid(broken));
        Assert.Null(new BodetReader().Read(broken));
    }

    [Fact]
    public void Reads_the_last_minute_as_tenths()
    {
        var state = new BodetReader().Read(Frame([(byte)'G', (byte)'1', (byte)'1', 0x80, Floorball,
            (byte)'5', (byte)'6', BodetProtocol.TenthSeparator, (byte)'4',
            (byte)'0', (byte)'0', (byte)'1', (byte)'0', (byte)'0', (byte)'0', (byte)'3']));

        Assert.NotNull(state);
        Assert.Equal("56.4", state.Time);
        Assert.True(state.IsLastMinute);
    }

    [Fact]
    public void Keeps_the_clock_while_a_penalty_frame_arrives()
    {
        var reader = new BodetReader();
        reader.Read(Match("17:35", 4, 1, 2));

        var state = reader.Read(Penalty(BodetProtocol.GuestPenaltyMessage, "1:29", "0:00"));

        Assert.NotNull(state);
        Assert.Equal("17:35", state.Time);
        Assert.Equal("4", state.HomeScore);
        Assert.Collection(state.GuestPenalties, only => Assert.Equal("01:29", only.Time));
    }

    [Fact]
    public void Takes_the_penalty_numbers_from_the_player_frame()
    {
        var reader = new BodetReader();
        reader.Read(Match("17:35", 0, 0, 1));
        reader.Read(Penalty(BodetProtocol.HomePenaltyMessage, "2:00", "0:00"));

        var state = reader.Read(Players(" 3", "12", "  ", "17", "  ", "  "));

        Assert.NotNull(state);
        Assert.Collection(state.HomePenalties, only =>
        {
            Assert.Equal("3", only.Player);
            Assert.Equal("02:00", only.Time);
        });
    }

    [Fact]
    public void Reassembles_a_frame_that_arrives_in_two_chunks()
    {
        var frame = Match("12:03", 2, 2, 3);
        var reader = new BodetReader();

        Assert.Null(reader.Read(frame[..9]));

        var state = reader.Read(frame[9..]);

        Assert.NotNull(state);
        Assert.Equal("12:03", state.Time);
    }

    [Fact]
    public void Reads_several_frames_from_one_chunk()
    {
        var chunk = Match("09:12", 3, 2, 3)
            .Concat(Penalty(BodetProtocol.HomePenaltyMessage, "1:05", "0:00"))
            .ToArray();

        var state = new BodetReader().Read(chunk);

        Assert.NotNull(state);
        Assert.Equal("09:12", state.Time);
        Assert.Collection(state.HomePenalties, only => Assert.Equal("01:05", only.Time));
    }

    [Fact]
    public void Writes_every_frame_to_the_log()
    {
        var logged = new BodetReader().Describe(Match("09:12", 3, 2, 3));

        Assert.Contains("01 7f 02 47 31 31", logged);
    }

    [Fact]
    public void Listens_on_tcp_for_a_bodet_clock_and_on_udp_for_icast()
    {
        Assert.Equal(ClockTransport.Tcp, ClockTypes.Transport(BodetProtocol.Type));
        Assert.Equal(BodetProtocol.DefaultPort, ClockTypes.DefaultPort("bodet"));
        Assert.IsType<BodetReader>(ClockTypes.Reader("Bodet"));
        Assert.Equal(ClockTransport.Udp, ClockTypes.Transport(ICastProtocol.Type));
        Assert.IsType<ICastReader>(ClockTypes.Reader(""));
        Assert.False(ClockTypes.Supported("Fabel"));
    }

    static byte[] Match(string clock, int home, int guest, int period)
    {
        var minutes = clock[..2];
        var seconds = clock[3..];
        return Frame(
        [
            (byte)'G', (byte)'1', (byte)'1', 0x80, Floorball,
            (byte)minutes[0], (byte)minutes[1], (byte)seconds[0], (byte)seconds[1],
            .. Padded(home), .. Padded(guest), (byte)('0' + period),
        ]);
    }

    static byte[] Penalty(int message, string first, string second)
    {
        var type = message.ToString();
        return Frame(
        [
            (byte)'G', (byte)type[0], (byte)type[1], Floorball,
            Indicator(first), (byte)first[0], (byte)first[2], (byte)first[3],
            Indicator(second), (byte)second[0], (byte)second[2], (byte)second[3],
        ]);
    }

    static byte[] Players(params string[] numbers) =>
        Frame([(byte)'G', (byte)'1', (byte)'5', Floorball,
            .. numbers.SelectMany(number => new[] { (byte)number[0], (byte)number[1] })]);

    static byte Indicator(string penalty) => penalty == "0:00" ? (byte)0x80 : (byte)0x81;

    static byte[] Padded(int score) => score.ToString("000").Select(c => (byte)c).ToArray();

    static byte[] Frame(byte[] body)
    {
        var frame = new byte[body.Length + 5];
        frame[0] = BodetProtocol.StartOfHeading;
        frame[1] = 0x7f;
        frame[2] = BodetProtocol.StartOfText;
        body.CopyTo(frame, 3);
        frame[^2] = BodetProtocol.EndOfText;
        frame[^1] = BodetFrame.Lrc(frame, frame.Length - 1);
        return frame;
    }
}
