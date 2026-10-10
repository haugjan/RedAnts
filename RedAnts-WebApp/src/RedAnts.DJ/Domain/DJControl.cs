namespace RedAnts.DJ.Domain;

public enum DJControl { Pause, Fade, Previous, Next }

public static class DJControls
{
    public static IReadOnlyList<DJControl> All { get; } = [DJControl.Pause, DJControl.Fade, DJControl.Previous, DJControl.Next];

    public static string Label(DJControl control, bool paused = false) => control switch
    {
        DJControl.Pause => paused ? "Weiter" : "Pause",
        DJControl.Fade => "Fade-out",
        DJControl.Previous => "Zurück",
        _ => "Vor",
    };

    public static string Icon(DJControl control, bool paused = false) => control switch
    {
        DJControl.Pause => paused ? "▶" : "⏸",
        DJControl.Fade => "🔉",
        DJControl.Previous => "⏮",
        _ => "⏭",
    };
}
