using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests.TabBoards;

public class TabBoardPhasesTests
{
    private static readonly (string Id, string Name)[] Music =
    [
        ("tor-ra", "Tor Red Ants"), ("tor-g", "Tor Gegner"), ("strafe-ra", "Strafe Red Ants"), ("strafe-g", "Strafe Gegner"),
        ("gastro", "Gastro Werbung"), ("pause", "Vor/Nach Spiel / Pause · Pause"),
    ];

    private static TabBoard Board() => TabBoardStart.Build(new ShowSetup([], [],
    [
        new Deck("xl", "Streamdeck XL", 8, 4,
        [
            new DeckPage(DeckPage.HomeId, "Start", null,
            [
                new DeckKey(1, KeyKinds.Special, "Tor", Special: SpecialKey.GoalHome, Steps: [new KeyStep(KeyActions.MusicPlay, "tor-ra")]),
                new DeckKey(2, KeyKinds.Special, "Tor", Special: SpecialKey.GoalAway),
            ]),
            new DeckPage("vor-lupl", "Vor Spiel L-UPL", DeckPage.HomeId,
            [
                new DeckKey(1, KeyKinds.Music, "1. Mobiliar Unihockey", MusicId: "mobi"),
                new DeckKey(2, KeyKinds.Music, "2. Wartesong bis Teams bereit", MusicId: "warte"),
                new DeckKey(3, KeyKinds.Music, "6. Starting Six Gegner", MusicId: "s6g"),
                new DeckKey(4, KeyKinds.Music, "8. Bully", MusicId: "bully"),
            ]),
            new DeckPage("vor-u21", "Vor Spiel U21", DeckPage.HomeId,
            [
                new DeckKey(1, KeyKinds.Music, "1. Wartesong bis Teams bereit", MusicId: "warte21"),
                new DeckKey(2, KeyKinds.Music, "4. Starting Six Gegner", MusicId: "s6g21"),
                new DeckKey(3, KeyKinds.Music, "6. Bully", MusicId: "bully21"),
            ]),
            new DeckPage("pause-lupl", "Pause L-UPL", DeckPage.HomeId, [new DeckKey(1, KeyKinds.Music, "2. Werbung Gastro", MusicId: "pause")]),
        ], PanicKey: 25, BackKey: 17),
    ]), new Dictionary<string, Hall>(), Music).Validated();

    private static ResolvedKey[] Tab(string tab, string league)
    {
        var board = Board();
        return [.. board.Resolve(board.Deck(TabBoardStart.XlDeck)!, tab, league)];
    }

    [Fact]
    public void The_entrance_is_a_numbered_run_that_closes_up_in_the_u21()
    {
        Assert.Equal(["Mobiliar Unihockey", "Wartesong bis Teams bereit", "Starting Six Gegner", "Bully"],
            Tab(BoardTabs.Before, TabBoardStart.Lupl).Where(k => k.Position is not null).Select(k => k.Button.Label));
        Assert.Equal([9, 10, 11], Tab(BoardTabs.Before, TabBoardStart.U21).Where(k => k.Position is not null).Select(k => k.Number));
    }

    [Fact]
    public void Starting_six_plays_its_league_music_and_then_asks_for_the_players()
    {
        var key = Tab(BoardTabs.Before, TabBoardStart.U21).Single(k => k.Button.Id == TabBoardPhases.StartingSix(false));

        Assert.Equal(SpecialKey.Starting6Away, key.Action.Special);
        Assert.Equal("s6g21", Assert.Single(key.Action.Sequence).Target);
    }

    [Fact]
    public void Red_ants_sit_left_and_the_opponent_right()
    {
        var during = Tab(BoardTabs.During, TabBoardStart.Lupl);

        Assert.Equal("tor-ra", during.Single(k => k.Number == 9).Action.Sequence.First().Target);
        Assert.Equal("tor-g", during.Single(k => k.Number == 13).Action.Sequence.First().Target);
        Assert.Equal(TabBoardPhases.GoalSpeaker(true), during.Single(k => k.Number == 10).Button.Id);
    }

    [Fact]
    public void Only_the_immediate_key_plays_the_goal_music()
    {
        var speaker = Board().Button(TabBoardPhases.GoalSpeaker(true))!;

        Assert.Equal(SpecialKey.GoalHome, speaker.Action.Special);
        Assert.DoesNotContain(speaker.Action.Sequence, s => s.Action == KeyActions.MusicPlay);
    }

    [Fact]
    public void The_gastro_advert_plays_the_gastro_jingle_in_every_league()
    {
        var gastro = Board().Button(TabBoardPhases.Gastro)!;

        Assert.Equal("gastro", gastro.Action.MusicId);
        Assert.Null(gastro.Leagues);
    }

    [Fact]
    public void Buttons_still_to_be_defined_are_placed_empty()
    {
        var pause = Tab(BoardTabs.Break, TabBoardStart.Lupl);

        Assert.Empty(pause.Single(k => k.Button.Id == TabBoardPhases.BreakGame).Action.Sequence);
    }

    [Fact]
    public void Every_tab_keeps_the_music_controls() =>
        Assert.All(BoardTabs.All, tab => Assert.Contains(Tab(tab.Id, TabBoardStart.Lupl), k => k.Number == 32));
}
