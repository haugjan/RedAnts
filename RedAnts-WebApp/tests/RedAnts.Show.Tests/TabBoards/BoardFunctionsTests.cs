using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests.TabBoards;

public class BoardFunctionsTests
{
    private static readonly Venue Win4 = new("win4", "WIN4", new HallDevices(), new HallCapabilities());
    private static readonly Venue Rennweg = new("rennweg", "Rennweg", new HallDevices(), new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: false));
    private static readonly League Lupl = new("lupl", "L-UPL", Livestream: true, Players: true);
    private static readonly League U21 = new("u21", "U21", Livestream: false, Players: false);

    [Fact]
    public void The_hall_and_the_league_give_the_defaults()
    {
        Assert.Equal(new BoardFunctions(true, true, true, true, true), BoardFunctions.Defaults(Win4, Lupl));
        Assert.Equal(new BoardFunctions(true, true, false, true, false), BoardFunctions.Defaults(Win4, U21));
        Assert.Equal(new BoardFunctions(true, false, false, false, false), BoardFunctions.Defaults(Rennweg, Lupl));
    }

    [Fact]
    public void A_function_the_hall_lacks_cannot_be_switched_on() =>
        Assert.False(BoardFunctions.Defaults(Rennweg, Lupl).With(BoardFunctions.LedId, true).Within(BoardFunctions.Available(Rennweg)).Led);

    [Fact]
    public void A_key_runs_while_one_of_its_parts_is_on()
    {
        var goal = new ButtonAction(KeyKinds.Steps, Steps: [new KeyStep(KeyActions.MusicPlay, "tor"), new KeyStep(KeyActions.LedStart, "led:tor-heim")]);
        var withoutLed = BoardFunctions.Defaults(Win4, Lupl) with { Led = false };

        Assert.True(withoutLed.Runs(goal));
        Assert.False((withoutLed with { Sound = false }).Runs(goal));
    }

    [Fact]
    public void A_vmix_title_is_off_without_livestream() =>
        Assert.False(BoardFunctions.Defaults(Win4, U21).Runs(new ButtonAction(KeyKinds.Steps, Steps: [new KeyStep(KeyActions.VmixShow, "title:coach")])));

    [Fact]
    public void Choosing_a_player_needs_somewhere_to_show_her()
    {
        var scorer = new ButtonAction(KeyKinds.Special, Special: SpecialKey.GoalHome, Livestream: false, Led: true);

        Assert.Equal([BoardFunctions.LedId, BoardFunctions.PlayersId], BoardFunctions.Uses(scorer).Order());
    }
}
