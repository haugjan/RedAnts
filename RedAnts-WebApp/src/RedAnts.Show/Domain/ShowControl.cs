namespace RedAnts.Show.Domain;

public enum ShowControl { Pause, Fade, Previous, Next }

public static class ShowControls
{
    public static IReadOnlyList<ShowControl> All { get; } = [ShowControl.Pause, ShowControl.Fade, ShowControl.Previous, ShowControl.Next];

    public static string Label(ShowControl control, bool paused = false) => control switch
    {
        ShowControl.Pause => paused ? "Weiter" : "Pause",
        ShowControl.Fade => "Fade-out",
        ShowControl.Previous => "Zurück",
        _ => "Vor",
    };

    public static string Icon(ShowControl control, bool paused = false) => control switch
    {
        ShowControl.Pause => paused ? "▶" : "⏸",
        ShowControl.Fade => "🔉",
        ShowControl.Previous => "⏮",
        _ => "⏭",
    };
}
