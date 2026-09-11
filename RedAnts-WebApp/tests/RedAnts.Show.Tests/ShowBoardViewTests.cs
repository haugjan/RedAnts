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
        Assert.Equal(ShowSlotKind.Pause, slots[13].Kind);
        Assert.Equal(ShowSlotKind.Fade, slots[14].Kind);
        Assert.False(slots[13].Enabled);
    }

    [Fact]
    public void Slots_reserve_the_first_cell_for_back_inside_a_folder()
    {
        var slots = ShowLayout.Slots([Tile("goal", 0, 0)], hasBack: true, n => n.Id == "goal", isPlaying: true, paused: false);

        Assert.Equal(ShowSlotKind.Back, slots[0].Kind);
        var goal = Assert.Single(slots, s => s.TileId == "goal");
        Assert.Equal(2, goal.Number);
        Assert.True(goal.Active);
        Assert.True(slots[14].Enabled);
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
