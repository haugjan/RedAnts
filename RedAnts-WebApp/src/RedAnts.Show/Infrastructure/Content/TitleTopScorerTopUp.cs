using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TitleTopScorerTopUp
{
    public const string SettingKey = "Show:TitleTopScorer";
    public const string Version = "1-imgtopscorer";

    public static ShowSetup? Fill(ShowSetup show) => TitleFieldTopUp.Fill(show, Hall.PlayerTitles, Hall.TopScorerFields);
}
