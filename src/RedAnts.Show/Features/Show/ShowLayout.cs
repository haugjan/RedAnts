namespace RedAnts.Features.Show;

public static class ShowLayout
{
    public const int Cols = 8;
    public const int Rows = 4;

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
