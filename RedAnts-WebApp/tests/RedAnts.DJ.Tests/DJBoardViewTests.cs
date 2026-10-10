using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Board;
using RedAnts.DJ.Features.Remote;
using RedAnts.DJ.Features.Remote.Infrastructure;
using Xunit;

namespace RedAnts.DJ.Tests;

public class DJBoardViewTests
{
    [Fact]
    public void Slots_cover_the_fifteen_board_cells_row_by_row()
    {
        var slots = DJLayout.Slots([Tile("goal", 1, 0), Tile("penalty", 0, 1)], hasBack: false, _ => false, isPlaying: false, paused: false);

        Assert.Equal(Enumerable.Range(1, 15), slots.Select(s => s.Number));
        Assert.Equal(DJSlotKind.Empty, slots[0].Kind);
        Assert.Equal("goal", slots[1].TileId);
        Assert.Equal("penalty", slots[5].TileId);
        Assert.Equal(DJSlotKind.Empty, slots[13].Kind);
        Assert.Equal(DJSlotKind.Empty, slots[14].Kind);
    }

    [Fact]
    public void Slots_reserve_the_first_cell_for_back_inside_a_folder()
    {
        var slots = DJLayout.Slots([Tile("goal", 0, 0)], hasBack: true, n => n.Id == "goal", isPlaying: true, paused: false);

        Assert.Equal(DJSlotKind.Back, slots[0].Kind);
        var goal = Assert.Single(slots, s => s.TileId == "goal");
        Assert.Equal(2, goal.Number);
        Assert.True(goal.Active);
    }

    [Fact]
    public void Control_tiles_sit_where_they_are_placed_and_only_work_while_playing()
    {
        var nodes = new[]
        {
            DJProfileLayout.ControlTile("prev", DJControl.Previous, 0, 0),
            DJProfileLayout.ControlTile("pause", DJControl.Pause, 2, 1),
        };

        var idle = DJLayout.Slots(nodes, hasBack: false, _ => false, isPlaying: false, paused: false);
        var paused = DJLayout.Slots(nodes, hasBack: false, _ => false, isPlaying: true, paused: true);

        Assert.Equal(DJSlotKind.Previous, idle[0].Kind);
        Assert.False(idle[0].Enabled);
        Assert.Equal(DJSlotKind.Pause, paused[7].Kind);
        Assert.True(paused[7].Enabled);
        Assert.Equal("Weiter", paused[7].Label);
    }

    [Fact]
    public void Legacy_profiles_get_pause_and_fade_on_every_level()
    {
        var folder = new DJButton("more", "Mehr", Children: [Tile("inner", 0, 0)]);
        var legacy = new DJProfile("p", "P", null, [Tile("goal", 3, 2), folder]);

        var upgraded = DJProfileLayout.Upgrade(legacy);

        Assert.Equal(DJProfileLayout.Current, upgraded.LayoutVersion);
        var rootSlots = DJLayout.Slots(upgraded.Root, hasBack: false, _ => false, isPlaying: true, paused: false);
        Assert.Equal(DJSlotKind.Pause, rootSlots[13].Kind);
        Assert.Equal(DJSlotKind.Fade, rootSlots[14].Kind);
        Assert.Contains(rootSlots, s => s.TileId == "goal");
        var inner = upgraded.Root.Single(n => n.Id == "more").Children!;
        Assert.Equal(["more-pause", "more-fade", "inner"], inner.Select(n => n.Id));
        Assert.Same(upgraded, DJProfileLayout.Upgrade(upgraded));
    }

    [Fact]
    public void Slots_show_folders_with_their_id()
    {
        var folder = new DJButton("more", "Mehr", Children: [Tile("inner", 0, 0)], X: 2, Y: 0);

        var slot = DJLayout.Slots([folder], hasBack: false, _ => false, isPlaying: false, paused: false)[2];

        Assert.Equal(DJSlotKind.Folder, slot.Kind);
        Assert.Equal("more", slot.TileId);
        Assert.Equal("📁", slot.Icon);
    }

    [Fact]
    public void Publishing_the_same_view_keeps_the_version()
    {
        var views = new DJBoardViews();
        var board = Guid.NewGuid();
        views.Publish(board, null, View("home"));
        var first = views.Current(null)!.Version;

        views.Publish(board, null, View("home"));

        Assert.Equal(first, views.Current(null)!.Version);
    }

    [Fact]
    public void Current_picks_the_board_of_the_room()
    {
        var views = new DJBoardViews();
        views.Publish(Guid.NewGuid(), "HalleA", View("a"));
        views.Publish(Guid.NewGuid(), "halleb", View("b"));

        Assert.Equal("a", views.Current(" halleA ")!.View.ProfileId);
        Assert.Equal("b", views.Current(null)!.View.ProfileId);
        Assert.Null(views.Current("hallec"));
    }

    [Fact]
    public void Withdrawing_the_board_clears_its_view()
    {
        var views = new DJBoardViews();
        var board = Guid.NewGuid();
        views.Publish(board, null, View("a"));

        views.Withdraw(board);

        Assert.Null(views.Current(null));
    }

    [Fact]
    public async Task Waiting_returns_as_soon_as_the_board_changes()
    {
        var views = new DJBoardViews();
        var board = Guid.NewGuid();
        views.Publish(board, null, View("a"));
        var since = views.Current(null)!.Version;

        var waiting = views.WaitForChangeAsync(null, since, TimeSpan.FromSeconds(10), CancellationToken.None);
        Assert.False(waiting.IsCompleted);
        views.Publish(board, null, View("b"));

        var changed = await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("b", changed!.View.ProfileId);
    }

    [Fact]
    public async Task Waiting_gives_up_after_the_timeout_with_the_unchanged_view()
    {
        var views = new DJBoardViews();
        views.Publish(Guid.NewGuid(), null, View("a"));
        var since = views.Current(null)!.Version;

        var result = await views.WaitForChangeAsync(null, since, TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.Equal(since, result!.Version);
    }

    private static DJButton Tile(string id, int x, int y) =>
        new(id, id, Sound: new DJSound(SoundKind.Local, "sounds/x.mp3"), X: x, Y: y);

    private static DJBoardView View(string profileId) =>
        new(profileId, profileId, null, [], [], null, false, true);
}
