namespace RedAnts.Features.Show;

// 12x8-Raster für die Board-Anordnung. Buttons haben Position (X,Y) und Grösse (W,H)
// in Rasterzellen; unpositionierte Buttons (X<0) fliessen automatisch ein.
public static class ShowLayout
{
    public const int Cols = 26;
    public const int Rows = 16;

    public static (int W, int H) DefaultSize(TileSize s) => s switch
    {
        TileSize.Wide => (8, 3),
        TileSize.Tall => (4, 6),
        TileSize.Big => (8, 6),
        _ => (4, 3),
    };

    public static (int W, int H) Dim(int w, int h, TileSize size)
    {
        var (dw, dh) = DefaultSize(size);
        return (Math.Clamp(w > 0 ? w : dw, 1, Cols), Math.Clamp(h > 0 ? h : dh, 1, Rows));
    }

    public static IReadOnlyDictionary<string, (int X, int Y, int W, int H)> Resolve(
        IReadOnlyList<ShowButton> nodes, bool reserveTopLeft)
    {
        var occ = new bool[Cols, Rows];
        var result = new Dictionary<string, (int, int, int, int)>();
        if (reserveTopLeft) Mark(occ, 0, 0, 4, 3);

        foreach (var n in nodes.Where(n => n is { X: >= 0, Y: >= 0 }))
        {
            var (w, h) = Dim(n.W, n.H, n.Size);
            var x = Math.Clamp(n.X, 0, Cols - w);
            var y = Math.Max(0, n.Y);
            Mark(occ, x, y, w, h);
            result[n.Id] = (x, y, w, h);
        }
        foreach (var n in nodes.Where(n => n.X < 0 || n.Y < 0))
        {
            var (w, h) = Dim(n.W, n.H, n.Size);
            var (x, y) = FindFree(occ, w, h);
            Mark(occ, x, y, w, h);
            result[n.Id] = (x, y, w, h);
        }
        return result;
    }

    // Weist EditButtons gültige Position/Grösse zu (mutierend), für den Editor-Canvas.
    public static void EnsureEdit(IReadOnlyList<EditButton> nodes)
    {
        var occ = new bool[Cols, Rows];
        foreach (var n in nodes.Where(n => n is { X: >= 0, Y: >= 0 }))
        {
            if (n.W <= 0 || n.H <= 0) { var (dw, dh) = DefaultSize(n.Size); if (n.W <= 0) n.W = dw; if (n.H <= 0) n.H = dh; }
            n.W = Math.Clamp(n.W, 1, Cols);
            n.H = Math.Clamp(n.H, 1, Rows);
            n.X = Math.Clamp(n.X, 0, Cols - n.W);
            n.Y = Math.Max(0, n.Y);
            Mark(occ, n.X, n.Y, n.W, n.H);
        }
        foreach (var n in nodes.Where(n => n.X < 0 || n.Y < 0))
        {
            if (n.W <= 0 || n.H <= 0) { var (dw, dh) = DefaultSize(n.Size); if (n.W <= 0) n.W = dw; if (n.H <= 0) n.H = dh; }
            n.W = Math.Clamp(n.W, 1, Cols);
            n.H = Math.Clamp(n.H, 1, Rows);
            var (x, y) = FindFree(occ, n.W, n.H);
            n.X = x; n.Y = y;
            Mark(occ, x, y, n.W, n.H);
        }
    }

    private static (int X, int Y) FindFree(bool[,] occ, int w, int h)
    {
        for (var y = 0; y + h <= Rows; y++)
            for (var x = 0; x + w <= Cols; x++)
                if (IsFree(occ, x, y, w, h)) return (x, y);

        var maxY = 0;
        for (var yy = 0; yy < Rows; yy++)
            for (var xx = 0; xx < Cols; xx++)
                if (occ[xx, yy]) maxY = Math.Max(maxY, yy + 1);
        return (0, maxY);
    }

    private static bool IsFree(bool[,] occ, int x, int y, int w, int h)
    {
        for (var yy = y; yy < y + h && yy < Rows; yy++)
            for (var xx = x; xx < x + w && xx < Cols; xx++)
                if (occ[xx, yy]) return false;
        return true;
    }

    private static void Mark(bool[,] occ, int x, int y, int w, int h)
    {
        for (var yy = y; yy < y + h && yy < Rows; yy++)
            for (var xx = x; xx < x + w && xx < Cols; xx++)
                occ[xx, yy] = true;
    }
}
