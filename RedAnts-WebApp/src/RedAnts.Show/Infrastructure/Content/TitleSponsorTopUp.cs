using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TitleSponsorTopUp
{
    public const string SettingKey = "Show:TitleSponsors";
    public const string Version = "1-tcunihockey";

    public static ShowSetup? Fill(ShowSetup show)
    {
        var titles = show.Titles.ToList();
        var changed = false;

        for (var index = 0; index < titles.Count; index++)
        {
            var stored = titles[index];
            if (!Hall.SponsoredTitles.Contains(stored.Kind, StringComparer.OrdinalIgnoreCase)) continue;

            var missing = Hall.SponsorFields.Where(field => !stored.Fields.ContainsKey(field.Key)).ToList();
            if (missing.Count == 0) continue;

            var fields = new Dictionary<string, string>(stored.Fields, StringComparer.OrdinalIgnoreCase);
            foreach (var (name, template) in missing) fields[name] = template;
            titles[index] = stored with { Fields = fields };
            changed = true;
        }

        return changed ? show with { Titles = titles } : null;
    }
}
