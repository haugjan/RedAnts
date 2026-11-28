using System.Text.Json;
using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests.TabBoards;

public class TabBoardTests
{
    private const string Lupl = "lupl";
    private const string U21 = "u21";

    private static readonly League[] Leagues = [new(Lupl, "L-UPL"), new(U21, "U21")];

    private static BoardButton Music(string id, params string[] leagues) =>
        new(id, id, "#2b3038", new ButtonAction(KeyKinds.Music, MusicId: id), Leagues: leagues.Length > 0 ? leagues : null);

    private static readonly BoardDeck Xl = new("tabs-xl", "XL", 8, 4, PanicKey: 25, BackKey: 17);

    private static readonly string[] Einlaufen =
        ["mobiliar", "wartesong", "lidl", "einlauf", "torbeschwoerung", "ss-gast", "ss-heim", "bully"];

    private static TabBoard Board(BoardLayout layout, params BoardDeck[] decks) => new(
        [],
        Leagues,
        Einlaufen.Select(id => id is "mobiliar" or "lidl" ? Music(id, Lupl) : Music(id))
            .Append(Music("logo")).Append(Music("nur-lupl", Lupl)).ToList(),
        [new TabLayout(BoardTabs.Before, 8, 4, layout)],
        decks.Length > 0 ? decks : [Xl]);

    private static BoardLayout Run(int start) =>
        new(Runs: [new NumberedRun("einlaufen", "Einlaufen", start, Einlaufen)]);

    [Fact]
    public void A_run_counts_every_step_in_the_league_that_has_them_all()
    {
        var keys = Board(Run(9)).Resolve(Xl, BoardTabs.Before, Lupl);

        Assert.Equal(Enumerable.Range(9, 8), keys.Select(k => k.Number));
        Assert.Equal(Enumerable.Range(1, 8), keys.Select(k => k.Position!.Value));
        Assert.Equal("mobiliar", keys[0].Button.Id);
    }

    [Fact]
    public void A_run_moves_up_and_renumbers_where_steps_are_missing()
    {
        var keys = Board(Run(9)).Resolve(Xl, BoardTabs.Before, U21);

        Assert.Equal(Enumerable.Range(9, 6), keys.Select(k => k.Number));
        Assert.Equal(Enumerable.Range(1, 6), keys.Select(k => k.Position!.Value));
        Assert.Equal(["wartesong", "einlauf", "torbeschwoerung", "ss-gast", "ss-heim", "bully"], keys.Select(k => k.Button.Id));
    }

    [Fact]
    public void A_fixed_key_keeps_its_place_and_leaves_a_gap_where_it_is_missing()
    {
        var layout = new BoardLayout(Keys: [new Placement(2, "nur-lupl"), new Placement(8, "logo")]);

        Assert.Equal([2, 8], Board(layout).Resolve(Xl, BoardTabs.Before, Lupl).Select(k => k.Number));
        Assert.Equal([8], Board(layout).Resolve(Xl, BoardTabs.Before, U21).Select(k => k.Number));
    }

    [Fact]
    public void A_run_moving_up_does_not_disturb_the_fixed_keys_around_it()
    {
        var layout = new BoardLayout(
            Keys: [new Placement(8, "logo")],
            Runs: [new NumberedRun("einlaufen", "Einlaufen", 9, Einlaufen)]);

        var keys = Board(layout).Resolve(Xl, BoardTabs.Before, U21);

        Assert.Contains(keys, k => k.Number == 8 && k.Button.Id == "logo");
        Assert.DoesNotContain(keys, k => k.Number is 15 or 16);
    }

    [Fact]
    public void A_run_on_the_small_deck_carries_on_into_the_next_row()
    {
        var mk2 = new BoardDeck("tabs-mk2", "Mk2", 5, 3, FollowsTab: false, Own: Run(1), PanicKey: 15, BackKey: 11);

        var keys = Board(BoardLayout.Empty, mk2).Resolve(mk2, BoardTabs.Before, Lupl);

        Assert.Equal(Enumerable.Range(1, 8), keys.Select(k => k.Number));
    }

    [Fact]
    public void A_button_plays_its_league_variant_and_otherwise_its_default()
    {
        var einlauf = Music("einlauf") with
        {
            PerLeague = new Dictionary<string, ButtonAction> { [U21] = new(KeyKinds.Music, MusicId: "sandstorm") },
        };

        Assert.Equal("einlauf", einlauf.ActionIn(Lupl).MusicId);
        Assert.Equal("sandstorm", einlauf.ActionIn(U21).MusicId);
    }

    [Fact]
    public void A_deck_that_does_not_follow_the_tab_shows_its_own_page_on_every_tab()
    {
        var pult = new BoardDeck("pult", "Pult", 5, 3, FollowsTab: false,
            Own: new BoardLayout(Keys: [new Placement(1, "logo")]), PanicKey: 15, BackKey: 11);
        var board = Board(Run(9), pult);

        foreach (var (tab, _) in BoardTabs.All)
            Assert.Equal("logo", Assert.Single(board.Resolve(pult, tab, Lupl)).Button.Id);
    }

    [Fact]
    public void The_fixed_layer_shows_on_every_tab()
    {
        var xl = Xl with { Fixed = new BoardLayout(Keys: [new Placement(32, "logo")]) };
        var board = Board(BoardLayout.Empty, xl);

        foreach (var (tab, _) in BoardTabs.All)
            Assert.Contains(board.Resolve(xl, tab, U21), k => k.Number == 32);
    }

    [Fact]
    public void The_board_knows_where_a_button_is_used()
    {
        var uses = Board(Run(9)).UsesOf("bully");

        Assert.Equal(["Vor dem Spiel · 8×4"], uses);
    }

    [Fact]
    public void A_valid_board_passes() => Board(Run(9)).Validated();

    [Fact]
    public void A_run_may_not_overlap_a_fixed_key() =>
        Assert.Throws<ValidationException>(() => Board(new BoardLayout(
            Keys: [new Placement(10, "logo")],
            Runs: [new NumberedRun("einlaufen", "Einlaufen", 9, Einlaufen)])).Validated());

    [Fact]
    public void A_key_may_not_sit_on_panic() =>
        Assert.Throws<ValidationException>(() => Board(new BoardLayout(Keys: [new Placement(25, "logo")])).Validated());

    [Fact]
    public void A_run_may_not_run_off_the_grid() =>
        Assert.Throws<ValidationException>(() => Board(Run(28)).Validated());

    [Fact]
    public void A_placed_button_must_exist() =>
        Assert.Throws<ValidationException>(() => Board(new BoardLayout(Keys: [new Placement(1, "gibts-nicht")])).Validated());

    [Fact]
    public void A_button_may_only_name_leagues_that_exist() =>
        Assert.Throws<ValidationException>(() => (Board(BoardLayout.Empty) with
        {
            Buttons = [Music("x", "cup")],
        }).Validated());

    [Fact]
    public void Two_buttons_may_not_share_an_id() =>
        Assert.Throws<ValidationException>(() => (Board(BoardLayout.Empty) with
        {
            Buttons = [Music("x"), Music("X")],
        }).Validated());

    [Fact]
    public void The_board_survives_a_trip_through_its_storage_format()
    {
        var plain = Board(Run(9));
        var board = plain with
        {
            Buttons = plain.Buttons.Select(b => b.Id == "einlauf"
                ? b with { PerLeague = new Dictionary<string, ButtonAction> { [U21] = new(KeyKinds.Music, MusicId: "sandstorm") } }
                : b).ToList(),
        };

        var back = JsonSerializer.Deserialize<TabBoard>(JsonSerializer.Serialize(board, ShowJson.Options), ShowJson.Options)!;

        Assert.Equal("sandstorm", back.Button("einlauf")!.ActionIn(U21).MusicId);
        Assert.Equal(board.Resolve(Xl, BoardTabs.Before, U21).Select(k => k.Number), back.Resolve(Xl, BoardTabs.Before, U21).Select(k => k.Number));
    }
}

public class BoardCompilerTests
{
    private static readonly BoardDeck Xl = new("tabs-xl", "XL", 8, 4, PanicKey: 25, BackKey: 17);

    private static TabBoard Board() => new(
        [],
        [new League("lupl", "L-UPL"), new League("u21", "U21")],
        [
            new BoardButton("mobiliar", "Mobiliar", "#111111", new ButtonAction(KeyKinds.Music, MusicId: "m"), Leagues: ["lupl"]),
            new BoardButton("bully", "Bully", "#222222", new ButtonAction(KeyKinds.Music, MusicId: "b"),
                PerLeague: new Dictionary<string, ButtonAction> { ["u21"] = new(KeyKinds.Music, MusicId: "b21") }),
        ],
        [new TabLayout(BoardTabs.Before, 8, 4, new BoardLayout(
            Runs: [new NumberedRun("einlaufen", "Einlaufen", 9, ["mobiliar", "bully"])]))],
        [Xl]);

    [Fact]
    public void A_tab_becomes_an_ordinary_deck_page_for_the_league()
    {
        var deck = Board().Compile(Xl, BoardTabs.Before, "u21", "xl");

        Assert.Equal(("xl", 25, 17), (deck.Id, deck.PanicKey, deck.BackKey));
        var key = Assert.Single(Assert.Single(deck.Pages).Keys);
        Assert.Equal((9, "1. Bully", "b21"), (key.Number, key.Label, key.MusicId));
    }

    [Fact]
    public void A_deck_without_anything_on_the_tab_is_left_alone() =>
        Assert.False(Board().Drives(Xl, BoardTabs.After));

    [Theory]
    [InlineData("U21", "u21")]
    [InlineData("L-UPL", "lupl")]
    [InlineData("Cup", "lupl")]
    [InlineData(null, "lupl")]
    public void The_game_picks_the_league(string? game, string league) =>
        Assert.Equal(league, Board().LeagueOf(game));
}
