using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Teams;

public sealed record TeamView(string Name, string Logo)
{
    public bool HasLogo => Logo.Length > 0;
}

public sealed class TeamResolver(ConfigStore store, DataFolder data)
{
    public TeamView Home(ClockState state) => Resolve(state.Home, store.Current.Teams.Home);

    public TeamView Guest(ClockState state) => Resolve(state.Guest, store.Current.Teams.Guest);

    public string LocalLogoPath(TeamView team)
    {
        var file = LogoFiles.FileOf(team.Logo);
        if (file is null) return "";
        var path = Path.Combine(data.Logos, file);
        return File.Exists(path) ? path : "";
    }

    TeamView Resolve(string abbreviation, TeamEntry? fallback)
    {
        var configured = store.Current.Teams.Find(abbreviation);
        if (configured is not null && (configured.Name.Length > 0 || configured.Logo.Length > 0))
            return new TeamView(configured.Name.Length > 0 ? configured.Name : abbreviation, configured.Logo);

        if (fallback is not null && (fallback.Name.Length > 0 || fallback.Logo.Length > 0))
            return new TeamView(fallback.Name.Length > 0 ? fallback.Name : abbreviation, fallback.Logo);

        return new TeamView(abbreviation, "");
    }
}
