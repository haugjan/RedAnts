using RedAnts.Domain;
using RedAnts.Game.Domain;
using Xunit;

namespace RedAnts.Game.Tests.Squad;

public class SquadLayoutRules
{
    [Fact]
    public void Squad_has_two_goalies_and_four_lines()
    {
        Assert.Equal(2, SquadLayout.Goalies);
        Assert.Equal(4, SquadLayout.Lines);
        Assert.Equal(5, SquadLayout.PerLine);
        Assert.Equal(22, SquadLayout.Slots);
    }

    [Theory]
    [InlineData(0, PlayerPosition.Goalie, 0)]
    [InlineData(1, PlayerPosition.Goalie, 0)]
    [InlineData(2, PlayerPosition.Defence, 1)]
    [InlineData(3, PlayerPosition.Defence, 1)]
    [InlineData(4, PlayerPosition.Forward, 1)]
    [InlineData(6, PlayerPosition.Forward, 1)]
    [InlineData(7, PlayerPosition.Defence, 2)]
    [InlineData(21, PlayerPosition.Forward, 4)]
    public void Every_slot_knows_its_role_and_line(int slot, PlayerPosition role, int line)
    {
        Assert.Equal(role, SquadLayout.RoleOf(slot));
        Assert.Equal(line, SquadLayout.LineOf(slot));
    }

    [Fact]
    public void Each_line_holds_two_defenders_and_three_forwards()
    {
        for (var line = 1; line <= SquadLayout.Lines; line++)
        {
            var slots = SquadLayout.SlotsOfLine(line);
            Assert.Equal(5, slots.Count);
            Assert.Equal(2, slots.Count(slot => SquadLayout.RoleOf(slot) == PlayerPosition.Defence));
            Assert.Equal(3, slots.Count(slot => SquadLayout.RoleOf(slot) == PlayerPosition.Forward));
        }
    }

    [Fact]
    public void Slots_outside_the_squad_are_rejected()
    {
        Assert.Throws<DomainException>(() => SquadLayout.RoleOf(SquadLayout.Slots));
        Assert.Throws<DomainException>(() => SquadLayout.RoleOf(-1));
    }
}

public class SquadPlacementRules
{
    private static Game.Domain.Squad Empty() =>
        Game.Domain.Squad.Create("token", new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(2)));

    [Fact]
    public void A_goalie_goes_on_a_goalie_slot()
    {
        var squad = Empty();
        squad.Place(0, PlayerPosition.Goalie, new SquadPick(1, 90), 1600);

        Assert.Equal(1, squad.Placed);
        Assert.Equal(90, squad.Spent);
    }

    [Fact]
    public void A_forward_does_not_go_on_a_defence_slot()
    {
        var squad = Empty();
        var error = Assert.Throws<DomainException>(() =>
            squad.Place(2, PlayerPosition.Forward, new SquadPick(1, 90), 1600));

        Assert.Contains("Verteidigung", error.Message);
    }

    [Fact]
    public void The_same_player_cannot_stand_twice()
    {
        var squad = Empty();
        squad.Place(4, PlayerPosition.Forward, new SquadPick(7, 50), 1600);

        Assert.Throws<DomainException>(() =>
            squad.Place(5, PlayerPosition.Forward, new SquadPick(7, 50), 1600));
    }

    [Fact]
    public void The_budget_caps_the_squad()
    {
        var squad = Empty();
        squad.Place(4, PlayerPosition.Forward, new SquadPick(1, 100), 150);

        var error = Assert.Throws<DomainException>(() =>
            squad.Place(5, PlayerPosition.Forward, new SquadPick(2, 100), 150));

        Assert.Contains("Budget", error.Message);
    }

    [Fact]
    public void Replacing_a_player_frees_the_previous_value()
    {
        var squad = Empty();
        squad.Place(4, PlayerPosition.Forward, new SquadPick(1, 140), 150);
        squad.Place(4, PlayerPosition.Forward, new SquadPick(2, 150), 150);

        Assert.Equal(150, squad.Spent);
        Assert.Equal(1, squad.Placed);
    }

    [Fact]
    public void Clearing_a_slot_gives_the_money_back()
    {
        var squad = Empty();
        squad.Place(0, PlayerPosition.Goalie, new SquadPick(1, 90), 1600);
        squad.Clear(0);

        Assert.Equal(0, squad.Spent);
        Assert.Equal(0, squad.Placed);
    }

    [Fact]
    public void A_full_squad_is_complete()
    {
        var squad = Empty();
        for (var slot = 0; slot < SquadLayout.Slots; slot++)
            squad.Place(slot, SquadLayout.RoleOf(slot), new SquadPick(slot + 1, 20), 1600);

        Assert.True(squad.IsComplete);
        Assert.Equal(SquadLayout.Slots, squad.Placed);
    }

    [Fact]
    public void A_blank_name_falls_back()
    {
        var squad = Empty();
        squad.Rename("   ");

        Assert.Equal("Mein Kader", squad.Name);
    }

    [Fact]
    public void A_long_name_is_cut()
    {
        var squad = Empty();
        squad.Rename(new string('a', 80));

        Assert.Equal(Game.Domain.Squad.NameMaxLength, squad.Name.Length);
    }

    [Fact]
    public void Restoring_keeps_only_real_slots()
    {
        var squad = Game.Domain.Squad.Restore("token", "Ameisen", DateTimeOffset.Now,
        [
            (0, new SquadPick(1, 40)),
            (99, new SquadPick(2, 40)),
        ]);

        Assert.Equal(1, squad.Placed);
        Assert.Equal(40, squad.Spent);
        Assert.Equal("Ameisen", squad.Name);
    }
}
