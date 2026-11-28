using RedAnts.Domain;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;
using Xunit;

namespace RedAnts.Show.Tests.TabBoards;

public class LayoutEditsTests
{
    private const string Lupl = "lupl";
    private const string U21 = "u21";

    private static readonly BoardDeck Xl = new("xl", "XL", 8, 4, PanicKey: 32, BackKey: 25);

    private static readonly BoardDeck Mk2 = new("mk2", "Mk2", 5, 3, FollowsTab: false, PanicKey: 15, BackKey: 11);

    private static BoardButton Music(string id, params string[] leagues) =>
        new(id, id, "#2b3038", new ButtonAction(KeyKinds.Music, MusicId: id), Leagues: leagues.Length > 0 ? leagues : null);

    private static TabBoard Board(BoardLayout layout, BoardDeck? deck = null) => new(
        [],
        [new League(Lupl, "L-UPL"), new League(U21, "U21")],
        [Music("mobiliar", Lupl), Music("wartesong"), Music("lidl", Lupl), Music("bully"), Music("logo"), Music("play")],
        [new TabLayout(BoardTabs.Before, 8, 4, layout)],
        [deck ?? Xl]);

    private static BoardLayout Entrance(int start) =>
        new(Runs: [new NumberedRun("einlaufen", "Einlaufen", start, ["mobiliar", "wartesong", "lidl", "bully"])]);

    [Fact]
    public void A_button_lands_on_a_free_slot()
    {
        var layout = LayoutEdits.Place(BoardLayout.Empty, Xl, 3, "logo");

        Assert.Equal(new Placement(3, "logo"), Assert.Single(layout.Placements));
    }

    [Fact]
    public void Placing_again_on_a_placed_slot_replaces_the_button_and_keeps_its_deck_flag()
    {
        var layout = LayoutEdits.ToggleOnDeck(LayoutEdits.Place(BoardLayout.Empty, Xl, 3, "logo"), 3);

        var replaced = LayoutEdits.Place(layout, Xl, 3, "bully");

        Assert.Equal(new Placement(3, "bully", OnDeck: false), Assert.Single(replaced.Placements));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(32)]
    [InlineData(33)]
    public void Nothing_lands_inside_a_run_on_panic_back_or_off_the_grid(int number)
    {
        Assert.Throws<ValidationException>(() => LayoutEdits.Place(Entrance(9), Xl, number, "logo"));
    }

    [Fact]
    public void The_layer_on_every_tab_blocks_its_slots_for_the_tab()
    {
        var always = new BoardLayout([new Placement(1, "play")]);

        Assert.Throws<ValidationException>(() => LayoutEdits.Place(BoardLayout.Empty, Xl, 1, "logo", always));
    }

    [Fact]
    public void A_run_grows_into_the_next_slot_and_stops_at_a_taken_one()
    {
        var layout = LayoutEdits.Place(Entrance(9), Xl, 14, "logo");

        var grown = LayoutEdits.AddToRun(layout, Xl, "einlaufen", "logo");

        Assert.Equal([9, 10, 11, 12, 13], LayoutEdits.Run(grown, "einlaufen")!.Span);
        Assert.Throws<ValidationException>(() => LayoutEdits.AddToRun(grown, Xl, "einlaufen", "bully"));
    }

    [Fact]
    public void A_run_on_a_small_deck_carries_on_into_the_next_row()
    {
        var (layout, id) = LayoutEdits.AddRun(BoardLayout.Empty, Mk2, "Einlaufen", 4);
        foreach (var button in new[] { "mobiliar", "wartesong", "lidl" })
            layout = LayoutEdits.AddToRun(layout, Mk2, id, button);

        var slots = LayoutEdits.Slots(Board(BoardLayout.Empty, Mk2), Mk2, layout);

        Assert.Equal([4, 5, 6], slots.Where(s => s.Kind == LayoutSlotKind.Run).Select(s => s.Number));
        Assert.Equal([1, 2, 3], slots.Where(s => s.Kind == LayoutSlotKind.Run).Select(s => s.Position!.Value));
    }

    [Fact]
    public void An_empty_run_holds_its_start_slot()
    {
        var (layout, id) = LayoutEdits.AddRun(BoardLayout.Empty, Xl, "Einlaufen", 9);

        Assert.False(LayoutEdits.IsFree(layout, Xl, 9));
        Assert.Equal(id, LayoutEdits.RunAt(layout, 9)!.Id);
    }

    [Fact]
    public void A_run_needs_a_name_and_a_free_start()
    {
        Assert.Throws<ValidationException>(() => LayoutEdits.AddRun(BoardLayout.Empty, Xl, " ", 9));
        Assert.Throws<ValidationException>(() => LayoutEdits.AddRun(Entrance(9), Xl, "Zweite", 11));
    }

    [Fact]
    public void Shifting_swaps_neighbours_and_ignores_the_edges()
    {
        var layout = LayoutEdits.Shift(Entrance(9), "einlaufen", 1, -1);
        var unchanged = LayoutEdits.Shift(layout, "einlaufen", 0, -1);

        Assert.Equal(["wartesong", "mobiliar", "lidl", "bully"], LayoutEdits.Run(unchanged, "einlaufen")!.Buttons);
    }

    [Fact]
    public void A_run_moves_only_onto_free_slots()
    {
        var layout = LayoutEdits.Place(Entrance(9), Xl, 20, "logo");

        Assert.Equal(10, LayoutEdits.Run(LayoutEdits.MoveRun(layout, Xl, "einlaufen", 10), "einlaufen")!.Start);
        Assert.Throws<ValidationException>(() => LayoutEdits.MoveRun(layout, Xl, "einlaufen", 18));
        Assert.Throws<ValidationException>(() => LayoutEdits.MoveRun(layout, Xl, "einlaufen", 30));
    }

    [Fact]
    public void A_placement_moves_onto_a_free_slot_only()
    {
        var layout = LayoutEdits.Place(Entrance(9), Xl, 1, "logo");

        Assert.Equal(2, Assert.Single(LayoutEdits.Move(layout, Xl, 1, 2).Placements).Number);
        Assert.Throws<ValidationException>(() => LayoutEdits.Move(layout, Xl, 1, 9));
    }

    [Fact]
    public void The_preview_moves_the_run_up_in_a_league_with_fewer_buttons()
    {
        var board = Board(Entrance(9));

        var slots = LayoutEdits.Preview(board, Xl, BoardTabs.Before, U21);

        var run = slots.Where(s => s.Kind == LayoutSlotKind.Run).ToList();
        Assert.Equal([9, 10], run.Select(s => s.Number));
        Assert.Equal(["wartesong", "bully"], run.Select(s => s.Button!.Id));
        Assert.Equal([1, 2], run.Select(s => s.Position!.Value));
        Assert.Equal(LayoutSlotKind.Reserved, slots.Single(s => s.Number == 25).Kind);
    }

    [Fact]
    public void Edited_layouts_pass_the_board_validation()
    {
        var (layout, id) = LayoutEdits.AddRun(LayoutEdits.Place(BoardLayout.Empty, Xl, 1, "logo"), Xl, "Einlaufen", 9);
        layout = LayoutEdits.AddToRun(layout, Xl, id, "mobiliar");
        layout = LayoutEdits.AddToRun(layout, Xl, id, "bully");

        Board(layout).Validated();
    }
}
