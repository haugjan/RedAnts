using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Remote;
using RedAnts.Show.Features.Remote.Infrastructure;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowBoardViewTests
{
    [Fact]
    public void Slots_cover_the_fifteen_board_cells_row_by_row()
    {
        var slots = ShowLayout.Slots([Tile("goal", 1, 0), Tile("penalty", 0, 1)], hasBack: false, _ => false, isPlaying: false, paused: false);

        Assert.Equal(Enumerable.Range(1, 15), slots.Select(s => s.Number));
        Assert.Equal(ShowSlotKind.Empty, slots[0].Kind);
        Assert.Equal("goal", slots[1].TileId);
        Assert.Equal("penalty", slots[5].TileId);
        Assert.Equal(ShowSlotKind.Empty, slots[13].Kind);
        Assert.Equal(ShowSlotKind.Empty, slots[14].Kind);
    }

    [Fact]
    public void Slots_reserve_the_first_cell_for_back_inside_a_folder()
    {
        var slots = ShowLayout.Slots([Tile("goal", 0, 0)], hasBack: true, n => n.Id == "goal", isPlaying: true, paused: false);

        Assert.Equal(ShowSlotKind.Back, slots[0].Kind);
        var goal = Assert.Single(slots, s => s.TileId == "goal");
        Assert.Equal(2, goal.Number);
        Assert.True(goal.Active);
    }

    [Fact]
    public void Control_tiles_sit_where_they_are_placed_and_only_work_while_playing()
    {
        var nodes = new[]
        {
            ShowProfileLayout.ControlTile("prev", ShowControl.Previous, 0, 0),
            ShowProfileLayout.ControlTile("pause", ShowControl.Pause, 2, 1),
        };

        var idle = ShowLayout.Slots(nodes, hasBack: false, _ => false, isPlaying: false, paused: false);
        var paused = ShowLayout.Slots(nodes, hasBack: false, _ => false, isPlaying: true, paused: true);

        Assert.Equal(ShowSlotKind.Previous, idle[0].Kind);
        Assert.False(idle[0].Enabled);
        Assert.Equal(ShowSlotKind.Pause, paused[7].Kind);
        Assert.True(paused[7].Enabled);
        Assert.Equal("Weiter", paused[7].Label);
    }

    [Fact]
    public void Legacy_profiles_get_pause_and_fade_on_every_level()
    {
        var folder = new ShowButton("more", "Mehr", Children: [Tile("inner", 0, 0)]);
        var legacy = new ShowProfile("p", "P", null, [Tile("goal", 3, 2), folder]);

        var upgraded = ShowProfileLayout.Upgrade(legacy);

        Assert.Equal(ShowProfileLayout.Current, upgraded.LayoutVersion);
        var rootSlots = ShowLayout.Slots(upgraded.Root, hasBack: false, _ => false, isPlaying: true, paused: false);
        Assert.Equal(ShowSlotKind.Pause, rootSlots[13].Kind);
        Assert.Equal(ShowSlotKind.Fade, rootSlots[14].Kind);
        Assert.Contains(rootSlots, s => s.TileId == "goal");
        var inner = upgraded.Root.Single(n => n.Id == "more").Children!;
        Assert.Equal(["more-pause", "more-fade", "inner"], inner.Select(n => n.Id));
        Assert.Same(upgraded, ShowProfileLayout.Upgrade(upgraded));
    }

    [Fact]
    public void Slots_show_folders_with_their_id()
    {
        var folder = new ShowButton("more", "Mehr", Children: [Tile("inner", 0, 0)], X: 2, Y: 0);

        var slot = ShowLayout.Slots([folder], hasBack: false, _ => false, isPlaying: false, paused: false)[2];

        Assert.Equal(ShowSlotKind.Folder, slot.Kind);
        Assert.Equal("more", slot.TileId);
        Assert.Equal("📁", slot.Icon);
    }

    [Fact]
    public void Publishing_the_same_view_keeps_the_version()
    {
        var views = new ShowBoardViews();
        var board = Guid.NewGuid();
        views.Publish(board, null, View("home"));
        var first = views.Current(null)!.Version;

        views.Publish(board, null, View("home"));

        Assert.Equal(first, views.Current(null)!.Version);
    }

    [Fact]
    public void Current_picks_the_board_of_the_room()
    {
        var views = new ShowBoardViews();
        views.Publish(Guid.NewGuid(), "HalleA", View("a"));
        views.Publish(Guid.NewGuid(), "halleb", View("b"));

        Assert.Equal("a", views.Current(" halleA ")!.View.ProfileId);
        Assert.Equal("b", views.Current(null)!.View.ProfileId);
        Assert.Null(views.Current("hallec"));
    }

    [Fact]
    public void Withdrawing_the_board_clears_its_view()
    {
        var views = new ShowBoardViews();
        var board = Guid.NewGuid();
        views.Publish(board, null, View("a"));

        views.Withdraw(board);

        Assert.Null(views.Current(null));
    }

    [Fact]
    public async Task Waiting_returns_as_soon_as_the_board_changes()
    {
        var views = new ShowBoardViews();
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
        var views = new ShowBoardViews();
        views.Publish(Guid.NewGuid(), null, View("a"));
        var since = views.Current(null)!.Version;

        var result = await views.WaitForChangeAsync(null, since, TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.Equal(since, result!.Version);
    }

    private static ShowButton Tile(string id, int x, int y) =>
        new(id, id, Sound: new ShowSound(SoundKind.Local, "sounds/x.mp3"), X: x, Y: y);

    private static ShowBoardView View(string profileId) =>
        new(profileId, profileId, null, [], [], null, false, true);
}
