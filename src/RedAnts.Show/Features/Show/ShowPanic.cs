namespace RedAnts.Features.Show;

public static class ShowPanic
{
    public const string Id = "panic";

    public static ShowButton Button(int x = 0, int y = 13, int w = 8, int h = 3)
        => new(Id, "PANIC", "⏹", "#E11330", TileSize.Big, null, null, null, null, x, y, w, h, true);

    public static IReadOnlyList<ShowButton> EnsureModel(IReadOnlyList<ShowButton> root)
    {
        if (root.Any(b => b.Panic)) return root;
        var list = root.ToList();
        list.Add(Button(-1, -1));
        return list;
    }

    public static void EnsureEdit(List<EditButton> root)
    {
        if (root.Any(b => b.Panic)) return;
        root.Add(new EditButton
        {
            Id = Id,
            Label = "PANIC",
            Icon = "⏹",
            Color = "#E11330",
            Size = TileSize.Big,
            Panic = true,
            X = -1, Y = -1, W = 8, H = 3,
        });
    }
}
