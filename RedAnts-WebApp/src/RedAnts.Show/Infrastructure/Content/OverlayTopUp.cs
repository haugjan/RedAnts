using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class OverlayTopUp
{
    public const string SettingKey = "Show:LedOverlay";
    public const string Version = "1-pause-intro";

    public const string Flag = " -o";

    public static readonly IReadOnlyList<string> Events = ["pause", "intro"];

    public static ShowSetup? Fill(ShowSetup show)
    {
        var templates = show.Led.ToList();
        var changed = false;

        for (var index = 0; index < templates.Count; index++)
        {
            var template = templates[index];
            if (!Events.Contains(template.Event, StringComparer.OrdinalIgnoreCase)) continue;
            if (Overlaid(template.Command)) continue;
            templates[index] = template with { Command = template.Command.TrimEnd() + Flag };
            changed = true;
        }

        return changed ? show with { Led = templates } : null;
    }

    private static bool Overlaid(string command) =>
        !command.TrimStart().StartsWith("start playlist", StringComparison.OrdinalIgnoreCase)
        || command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("-o");
}
