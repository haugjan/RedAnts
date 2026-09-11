namespace RedAnts.Show.Domain;

public static class ShowProfileLayout
{
    public const int Current = 2;

    private const string RootLevel = "root";
    private static readonly (int X, int Y) LegacyPauseCell = (3, 2);
    private static readonly (int X, int Y) LegacyFadeCell = (4, 2);

    public static ShowProfile Upgrade(ShowProfile profile) =>
        profile.LayoutVersion >= Current
            ? profile
            : profile with { Root = WithLegacyControls(profile.Root, RootLevel), LayoutVersion = Current };

    public static ShowButton ControlTile(string id, ShowControl control, int x = -1, int y = -1) =>
        new(id, ShowControls.Label(control), ShowControls.Icon(control), X: x, Y: y, Control: control);

    private static IReadOnlyList<ShowButton> WithLegacyControls(IReadOnlyList<ShowButton> nodes, string levelId)
    {
        var controls = new[]
        {
            ControlTile($"{levelId}-pause", ShowControl.Pause, LegacyPauseCell.X, LegacyPauseCell.Y),
            ControlTile($"{levelId}-fade", ShowControl.Fade, LegacyFadeCell.X, LegacyFadeCell.Y),
        };
        var upgraded = nodes.Select(n => n.IsFolder ? n with { Children = WithLegacyControls(n.Children!, n.Id) } : n);
        return controls.Concat(upgraded).ToList();
    }
}
