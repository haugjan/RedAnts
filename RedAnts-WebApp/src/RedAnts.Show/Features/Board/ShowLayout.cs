using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Remote;

namespace RedAnts.Show.Features.Board;

public static class ShowLayout
{
    public const int Cols = 5;
    public const int Rows = 3;

    public static readonly (int X, int Y) BackCell = (0, 0);
    public static readonly (int X, int Y) PauseCell = (Cols - 2, Rows - 1);
    public static readonly (int X, int Y) FadeCell = (Cols - 1, Rows - 1);

    public static bool IsReserved(int x, int y, bool hasBack) =>
        (x, y) == PauseCell || (x, y) == FadeCell || (hasBack && (x, y) == BackCell);

    public static IReadOnlyDictionary<string, (int X, int Y, int W, int H)> Resolve(
        IReadOnlyList<ShowButton> nodes, bool reserveTopLeft)
    {
        var occ = ReservedSet(reserveTopLeft);
        var result = new Dictionary<string, (int, int, int, int)>();

        foreach (var n in nodes.Where(n => n is { X: >= 0, Y: >= 0 }))
        {
            var (x, y) = Place(occ, Math.Clamp(n.X, 0, Cols - 1), Math.Max(0, n.Y));
            occ.Add((x, y));
            result[n.Id] = (x, y, 1, 1);
        }
        foreach (var n in nodes.Where(n => n.X < 0 || n.Y < 0))
        {
            var (x, y) = FindFree(occ);
            occ.Add((x, y));
            result[n.Id] = (x, y, 1, 1);
        }
        return result;
    }

    public static IReadOnlyList<ShowBoardSlot> Slots(
        IReadOnlyList<ShowButton> nodes, bool hasBack, Func<ShowButton, bool> isActive, bool isPlaying, bool paused)
    {
        var layout = Resolve(nodes, hasBack);
        var byCell = new Dictionary<(int X, int Y), ShowButton>();
        foreach (var node in nodes)
            if (layout.TryGetValue(node.Id, out var pos))
                byCell[(pos.X, pos.Y)] = node;

        var slots = new List<ShowBoardSlot>(Cols * Rows);
        for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Cols; x++)
                slots.Add(SlotAt(y * Cols + x + 1, (x, y), byCell, hasBack, isActive, isPlaying, paused));
        return slots;
    }

    private static ShowBoardSlot SlotAt(
        int number, (int X, int Y) cell, Dictionary<(int X, int Y), ShowButton> byCell,
        bool hasBack, Func<ShowButton, bool> isActive, bool isPlaying, bool paused)
    {
        if (hasBack && cell == BackCell) return new(number, ShowSlotKind.Back, null, "Zurück", "↩", null, false, true);
        if (cell == PauseCell) return new(number, ShowSlotKind.Pause, null, paused ? "Weiter" : "Pause", paused ? "▶" : "⏸", null, false, isPlaying);
        if (cell == FadeCell) return new(number, ShowSlotKind.Fade, null, "Fade-out", "🔉", null, false, isPlaying);
        if (!byCell.TryGetValue(cell, out var node)) return new(number, ShowSlotKind.Empty, null, "", null, null, false, false);
        if (node.IsFolder) return new(number, ShowSlotKind.Folder, node.Id, node.Label, node.Icon ?? "📁", node.Color, false, true);
        var playable = node.EffectiveSongs.Any(s => !string.IsNullOrWhiteSpace(s.Ref));
        return new(number, ShowSlotKind.Tile, node.Id, node.Label, node.Icon ?? "🔊", node.Color, isActive(node), playable);
    }

    public static void EnsureEdit(IReadOnlyList<EditButton> nodes, bool reserveBack)
    {
        var occ = ReservedSet(reserveBack);
        foreach (var n in nodes.Where(n => n is { X: >= 0, Y: >= 0 }))
        {
            var (x, y) = Place(occ, Math.Clamp(n.X, 0, Cols - 1), Math.Max(0, n.Y));
            n.X = x; n.Y = y; n.W = 1; n.H = 1;
            occ.Add((x, y));
        }
        foreach (var n in nodes.Where(n => n.X < 0 || n.Y < 0))
        {
            var (x, y) = FindFree(occ);
            n.X = x; n.Y = y; n.W = 1; n.H = 1;
            occ.Add((x, y));
        }
    }

    private static HashSet<(int, int)> ReservedSet(bool hasBack)
    {
        var set = new HashSet<(int, int)> { PauseCell, FadeCell };
        if (hasBack) set.Add(BackCell);
        return set;
    }

    private static (int X, int Y) Place(HashSet<(int, int)> occ, int x, int y) =>
        occ.Contains((x, y)) ? FindFree(occ) : (x, y);

    private static (int X, int Y) FindFree(HashSet<(int, int)> occ)
    {
        for (var y = 0; ; y++)
            for (var x = 0; x < Cols; x++)
                if (!occ.Contains((x, y))) return (x, y);
    }
}
