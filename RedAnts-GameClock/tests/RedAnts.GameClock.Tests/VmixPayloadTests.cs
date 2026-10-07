using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Vmix;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class VmixPayloadTests
{
    static readonly VmixScene Scene = new("Red Ants Winterthur", "Unihockey Berner Oberland", "", "", "#141414");

    static ClockState State(string time, string period, string homePenalty = "", string guestPenalty = "") =>
        new(time, "2", "6", period, "RED", "UBO", ClockState.GameTime,
            ClockState.Penalties(homePenalty), ClockState.Penalties(guestPenalty),
            [], "", DateTime.Now);

    [Fact]
    public void CarriesTimeScoreAndNames()
    {
        var payload = VmixPayload.Build(State("19:38", "3"), Scene);

        Assert.Equal("19:38", payload.Text[VmixSlots.Time]);
        Assert.Equal("2", payload.Text[VmixSlots.HomeScore]);
        Assert.Equal("6", payload.Text[VmixSlots.AwayScore]);
        Assert.Equal("Red Ants Winterthur", payload.Text[VmixSlots.HomeName]);
        Assert.Equal("Unihockey Berner Oberland", payload.Text[VmixSlots.AwayName]);
        Assert.Equal("3", payload.Text[VmixSlots.Period]);
    }

    [Theory]
    [InlineData("4", "O")]
    [InlineData("5", "P")]
    [InlineData("1", "1")]
    public void NamesOvertimeAndShootoutTheWayTcunihockeyDoes(string period, string expected) =>
        Assert.Equal(expected, VmixPayload.Build(State("19:38", period), Scene).Text[VmixSlots.Period]);

    [Fact]
    public void SplitsTheClockIntoTheFiveDigitFieldsOfTheLuplTitle()
    {
        var fields = VmixSlots.Expand("TxtClockTime{0}.Text", "19:38").ToArray();

        Assert.Equal(
        [
            ("TxtClockTimeM10.Text", "1"),
            ("TxtClockTimeM01.Text", "9"),
            ("TxtClockTimeT.Text", ":"),
            ("TxtClockTimeS10.Text", "3"),
            ("TxtClockTimeS01.Text", "8"),
        ], fields);
    }

    [Fact]
    public void PadsTheLastMinuteSoTheDigitsStayInPlace()
    {
        var fields = VmixSlots.Expand("TxtClockTime{0}.Text", "59.9").ToArray();

        Assert.Equal("5", fields[0].Value);
        Assert.Equal("9", fields[1].Value);
        Assert.Equal(".", fields[2].Value);
        Assert.Equal("9", fields[3].Value);
        Assert.Equal("", fields[4].Value);
    }

    [Fact]
    public void BlanksEveryDigitWhenThereIsNoPenalty()
    {
        var fields = VmixSlots.Expand("TxtHomePen1{0}.Text", "").ToArray();

        Assert.All(fields, field => Assert.Equal("", field.Value));
        Assert.Equal(5, fields.Length);
    }

    [Fact]
    public void SendsTheWholeValueWhenTheFieldHasNoDigitMarker()
    {
        var fields = VmixSlots.Expand("Clock.Text", "19:38").ToArray();

        Assert.Single(fields);
        Assert.Equal(("Clock.Text", "19:38"), fields[0]);
    }

    [Fact]
    public void ShowsTheStrafboxOnlyWhileAPenaltyRuns()
    {
        var running = VmixPayload.Build(State("19:38", "3", homePenalty: "7 01:43"), Scene);
        var over = VmixPayload.Build(State("19:38", "3"), Scene);

        Assert.Equal("01:43", running.Text[VmixSlots.HomePenalty1]);
        Assert.Equal("#141414FF", running.Colors[VmixSlots.HomePenalty1Fill]);
        Assert.Equal("", over.Text[VmixSlots.HomePenalty1]);
        Assert.Equal("#14141400", over.Colors[VmixSlots.HomePenalty1Fill]);
    }

    [Fact]
    public void TreatsAZeroPenaltyAsNoPenalty()
    {
        var payload = VmixPayload.Build(State("19:38", "3", homePenalty: "7 00:00"), Scene);

        Assert.Equal("", payload.Text[VmixSlots.HomePenalty1]);
        Assert.Equal("#14141400", payload.Colors[VmixSlots.HomePenalty1Fill]);
    }

    [Fact]
    public void CoversEverySlotOfTheLuplPreset()
    {
        var preset = VmixSlots.LuplPreset();
        var payload = VmixPayload.Build(State("19:38", "3"), Scene);
        var produced = payload.Text.Keys.Concat(payload.Colors.Keys).Concat(payload.Images.Keys).ToHashSet();

        Assert.All(VmixSlots.All, slot => Assert.True(preset.ContainsKey(slot.Key), slot.Key));
        Assert.All(VmixSlots.All, slot => Assert.True(produced.Contains(slot.Key), slot.Key));
    }
}
