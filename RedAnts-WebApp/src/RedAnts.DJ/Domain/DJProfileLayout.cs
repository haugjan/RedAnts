namespace RedAnts.DJ.Domain;

public static class DJProfileLayout
{
    public const int Current = 2;

    private const string RootLevel = "root";
    private static readonly (int X, int Y) LegacyPauseCell = (3, 2);
    private static readonly (int X, int Y) LegacyFadeCell = (4, 2);

    public static DJProfile Upgrade(DJProfile profile) =>
        profile.LayoutVersion >= Current
            ? profile
            : profile with { Root = WithLegacyControls(profile.Root, RootLevel), LayoutVersion = Current };

    public static DJButton ControlTile(string id, DJControl control, int x = -1, int y = -1) =>
        new(id, DJControls.Label(control), DJControls.Icon(control), X: x, Y: y, Control: control);

    private static IReadOnlyList<DJButton> WithLegacyControls(IReadOnlyList<DJButton> nodes, string levelId)
    {
        var controls = new[]
        {
            ControlTile($"{levelId}-pause", DJControl.Pause, LegacyPauseCell.X, LegacyPauseCell.Y),
            ControlTile($"{levelId}-fade", DJControl.Fade, LegacyFadeCell.X, LegacyFadeCell.Y),
        };
        var upgraded = nodes.Select(n => n.IsFolder ? n with { Children = WithLegacyControls(n.Children!, n.Id) } : n);
        return controls.Concat(upgraded).ToList();
    }
}
