using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TitleLogoTopUp
{
    public const string SettingKey = "Show:TitleTeamLogos";
    public const string Version = "1-lupl-damen";

    public static ShowSetup? Fill(ShowSetup show)
    {
        var titles = show.Titles.ToList();
        var changed = false;

        for (var index = 0; index < titles.Count; index++)
        {
            var stored = titles[index];
            if (Shipped(stored.Kind) is not { } shipped) continue;

            var missing = shipped.Fields
                .Where(field => IsImage(field.Key) && !stored.Fields.ContainsKey(field.Key))
                .ToList();
            if (missing.Count == 0) continue;

            var fields = new Dictionary<string, string>(stored.Fields, StringComparer.OrdinalIgnoreCase);
            foreach (var (name, template) in missing) fields[name] = template;
            titles[index] = stored with { Fields = fields };
            changed = true;
        }

        return changed ? show with { Titles = titles } : null;
    }

    private static OverlayTitle? Shipped(string kind) =>
        Hall.DefaultOverlayTitles.FirstOrDefault(title => title.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));

    private static bool IsImage(string field) =>
        field.StartsWith("Img", StringComparison.OrdinalIgnoreCase)
        && field.EndsWith(".Source", StringComparison.OrdinalIgnoreCase);
}
