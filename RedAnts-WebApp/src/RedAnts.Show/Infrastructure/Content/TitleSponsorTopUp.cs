using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TitleSponsorTopUp
{
    public const string SettingKey = "Show:TitleSponsors";
    public const string Version = "1-tcunihockey";

    public static ShowSetup? Fill(ShowSetup show) => TitleFieldTopUp.Fill(show, Hall.SponsoredTitles, Hall.SponsorFields);
}
