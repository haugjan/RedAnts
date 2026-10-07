using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Configuration;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class BodetScorepadProtocolTests
{
    static readonly byte[] Documented =
    [
        0x01, 0x7f, 0x02, 0x47, 0x31, 0x31, 0x80, 0x37, 0x20, 0x34, 0x30,
        0x37, 0x20, 0x30, 0x31, 0x20, 0x30, 0x30, 0x31, 0x03, 0x2d,
    ];

    readonly BodetScorepadProtocol _protocol = new();

    IClockParser Parser() => _protocol.CreateParser(new ClockSourceConfig { Protocol = BodetScorepadProtocol.Id });

    [Fact]
    public void ReadsTheDocumentedFrame()
    {
        var state = Parser().Read(Documented);

        Assert.NotNull(state);
        Assert.Equal("04:07", state.Time);
        Assert.Equal("1", state.HomeScore);
        Assert.Equal("0", state.GuestScore);
        Assert.Equal("1", state.Period);
    }

    [Fact]
    public void ComputesTheLrcTheWayTheManualDescribesIt() =>
        Assert.Equal(0x2d, BodetFrame.Lrc(Documented, Documented.Length - 1));

    [Fact]
    public void AcceptsOnlyFramesWithAMatchingLrc()
    {
        var broken = Documented.ToArray();
        broken[^1] ^= 0x01;

        Assert.True(BodetFrame.IsValid(Documented));
        Assert.False(BodetFrame.IsValid(broken));
        Assert.Null(Parser().Read(broken));
    }

    [Fact]
    public void TakesTheClockTheScoreAndThePeriodFromMessageEleven()
    {
        var state = Parser().Read(Match("17:35", 4, 1, 2));

        Assert.NotNull(state);
        Assert.Equal("17:35", state.Time);
        Assert.Equal("4", state.HomeScore);
        Assert.Equal("1", state.GuestScore);
        Assert.Equal("2", state.Period);
    }

    [Fact]
    public void ReadsThreeDigitScores()
    {
        var state = Parser().Read(Match("10:00", 110, 102, 3));

        Assert.NotNull(state);
        Assert.Equal("110", state.HomeScore);
        Assert.Equal("102", state.GuestScore);
    }

    [Fact]
    public void ReadsTheLastMinuteAsTenths()
    {
        var state = Parser().Read(Frame([(byte)'G', (byte)'1', (byte)'1', 0x80, BodetScorepadProtocol.Floorball,
            (byte)'5', (byte)'6', BodetScorepadProtocol.TenthSeparator, (byte)'4',
            (byte)'0', (byte)'0', (byte)'1', (byte)'0', (byte)'0', (byte)'0', (byte)'3']));

        Assert.NotNull(state);
        Assert.Equal("56.4", state.Time);
        Assert.True(state.IsLastMinute);
    }

    [Fact]
    public void ReadsBothPenaltiesOfTheHomeTeam()
    {
        var parser = Parser();
        parser.Read(Match("17:35", 0, 0, 1));

        var state = parser.Read(Penalty(BodetScorepadProtocol.HomePenaltyMessage, "1:34", "1:56"));

        Assert.NotNull(state);
        Assert.Collection(state.HomePenalties,
            first => Assert.Equal("01:34", first.Time),
            second => Assert.Equal("01:56", second.Time));
        Assert.Empty(state.GuestPenalties);
    }

    [Fact]
    public void KeepsTheClockWhileAPenaltyMessageArrives()
    {
        var parser = Parser();
        parser.Read(Match("17:35", 4, 1, 2));

        var state = parser.Read(Penalty(BodetScorepadProtocol.GuestPenaltyMessage, "1:29", "0:00"));

        Assert.NotNull(state);
        Assert.Equal("17:35", state.Time);
        Assert.Equal("4", state.HomeScore);
        Assert.Collection(state.GuestPenalties, only => Assert.Equal("01:29", only.Time));
    }

    [Fact]
    public void ReadsTheThirdPenaltyOfBothTeamsFromMessageFourteen()
    {
        var parser = Parser();
        parser.Read(Match("17:35", 0, 0, 1));
        parser.Read(Penalty(BodetScorepadProtocol.HomePenaltyMessage, "2:00", "1:00"));

        var state = parser.Read(Penalty(BodetScorepadProtocol.ThirdPenaltyMessage, "0:37", "1:08"));

        Assert.NotNull(state);
        Assert.Equal(["02:00", "01:00", "00:37"], state.HomePenalties.Select(p => p.Time));
        Assert.Collection(state.GuestPenalties, only => Assert.Equal("01:08", only.Time));
    }

    [Fact]
    public void TakesThePenaltyPlayerNumbersFromMessageFifteen()
    {
        var parser = Parser();
        parser.Read(Match("17:35", 0, 0, 1));
        parser.Read(Penalty(BodetScorepadProtocol.HomePenaltyMessage, "2:00", "0:00"));
        parser.Read(Penalty(BodetScorepadProtocol.GuestPenaltyMessage, "1:12", "0:00"));

        var state = parser.Read(Players(" 3", "12", "  ", "17", "  ", "  "));

        Assert.NotNull(state);
        Assert.Collection(state.HomePenalties, only =>
        {
            Assert.Equal("3", only.Player);
            Assert.Equal("02:00", only.Time);
        });
        Assert.Collection(state.GuestPenalties, only => Assert.Equal("17", only.Player));
    }

    [Fact]
    public void TreatsAZeroPenaltyAsNoPenalty()
    {
        var parser = Parser();
        parser.Read(Match("17:35", 0, 0, 1));

        var state = parser.Read(Penalty(BodetScorepadProtocol.HomePenaltyMessage, "0:00", "0:00"));

        Assert.NotNull(state);
        Assert.Empty(state.HomePenalties);
    }

    [Fact]
    public void ReassemblesAFrameThatArrivesInTwoChunks()
    {
        var frame = Match("12:03", 2, 2, 3);
        var parser = Parser();

        Assert.Null(parser.Read(frame[..9]));

        var state = parser.Read(frame[9..]);

        Assert.NotNull(state);
        Assert.Equal("12:03", state.Time);
    }

    [Fact]
    public void ReadsSeveralFramesFromOneChunk()
    {
        var chunk = Match("09:12", 3, 2, 3)
            .Concat(Penalty(BodetScorepadProtocol.HomePenaltyMessage, "1:05", "0:00"))
            .ToArray();

        var state = Parser().Read(chunk);

        Assert.NotNull(state);
        Assert.Equal("09:12", state.Time);
        Assert.Collection(state.HomePenalties, only => Assert.Equal("01:05", only.Time));
    }

    [Fact]
    public void SendsNoTeamAbbreviations()
    {
        var state = Parser().Read(Documented);

        Assert.NotNull(state);
        Assert.Equal("", state.Home);
        Assert.Equal("", state.Guest);
    }

    [Fact]
    public void FindsTheSportCodeWhereTheManualPutsIt()
    {
        Assert.Equal(BodetScorepadProtocol.Floorball, BodetFrame.SportOf(Documented));
        Assert.Equal(BodetScorepadProtocol.Floorball, BodetFrame.SportOf(Penalty(BodetScorepadProtocol.HomePenaltyMessage, "1:00", "0:00")));
    }

    [Fact]
    public void RecognisesItsOwnFramesAndNothingElse()
    {
        Assert.True(_protocol.Match(Documented) >= 80);
        Assert.Equal(0, _protocol.Match([0x41, 0x42, 0x43]));
        Assert.Equal(ClockTransport.Tcp, _protocol.Transport);
        Assert.Equal(4001, _protocol.DefaultPort);
    }

    static byte[] Match(string clock, int home, int guest, int period)
    {
        var minutes = clock[..2];
        var seconds = clock[3..];
        return Frame(
        [
            (byte)'G', (byte)'1', (byte)'1', 0x80, BodetScorepadProtocol.Floorball,
            (byte)minutes[0], (byte)minutes[1], (byte)seconds[0], (byte)seconds[1],
            .. Padded(home), .. Padded(guest), (byte)('0' + period),
        ]);
    }

    static byte[] Penalty(int message, string first, string second)
    {
        var type = message.ToString();
        return Frame(
        [
            (byte)'G', (byte)type[0], (byte)type[1], BodetScorepadProtocol.Floorball,
            Indicator(first), (byte)first[0], (byte)first[2], (byte)first[3],
            Indicator(second), (byte)second[0], (byte)second[2], (byte)second[3],
        ]);
    }

    static byte[] Players(params string[] numbers) =>
        Frame([(byte)'G', (byte)'1', (byte)'5', BodetScorepadProtocol.Floorball,
            .. numbers.SelectMany(number => new[] { (byte)number[0], (byte)number[1] })]);

    static byte Indicator(string penalty) => penalty == "0:00" ? (byte)0x80 : (byte)0x81;

    static byte[] Padded(int score) => score.ToString("000").Select(c => (byte)c).ToArray();

    static byte[] Frame(byte[] body)
    {
        var frame = new byte[body.Length + 5];
        frame[0] = BodetScorepadProtocol.StartOfHeading;
        frame[1] = 0x7f;
        frame[2] = BodetScorepadProtocol.StartOfText;
        body.CopyTo(frame, 3);
        frame[^2] = BodetScorepadProtocol.EndOfText;
        frame[^1] = BodetFrame.Lrc(frame, frame.Length - 1);
        return frame;
    }
}
