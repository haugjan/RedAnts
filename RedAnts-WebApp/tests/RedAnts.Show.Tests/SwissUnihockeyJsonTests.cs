using RedAnts.Show.Domain;
using RedAnts.Show.Features.Games.Infrastructure;
using Xunit;

namespace RedAnts.Show.Tests;

public class SwissUnihockeyJsonTests
{
    private const string Games = """
        {"type":"table","data":{"regions":[{"rows":[
          {"link":{"type":"web","page":"game_detail","ids":[1095929]},"cells":[
            {"text":["13.09.2026","19:30"]},
            {"text":["3-Fachhalle","Oberdorf BL"]},
            {"text":["SV Waldenburg Eagles"]},
            {"text":["Red Ants Winterthur"]},
            {"text":["0:21"]}]},
          {"cells":[{"text":["ohne Link"]}]}
        ]}]}}
        """;

    private const string Detail = """
        {"data":{"headers":[{"key":"home_logo"},{"key":"home_name"},{"key":"away_logo"},{"key":"away_name"},{"key":"result"},{"key":"date"},{"key":"time"},{"key":"location"},{"key":"first_referee"},{"key":"second_referee"}],
        "title":"SV Waldenburg Eagles - Red Ants Winterthur","subtitle":"Mobiliar Unihockey Cup Frauen 1/16-Final 2026/27",
        "regions":[{"rows":[{"cells":[
          {"image":{"url":"https://img/home.png"},"link":{"ids":[431615]}},
          {"text":["SV Waldenburg Eagles"],"link":{"ids":[431615]}},
          {"image":{"url":"https://img/away.png"},"link":{"ids":[428665]}},
          {"text":["Red Ants Winterthur"],"link":{"ids":[428665]}},
          {"text":["0:21"]},
          {"text":["13.09.2026"]},
          {"text":["19:30"]},
          {"text":["3-Fachhalle","Oberdorf BL"]},
          {"text":["Hans Muster"]},
          {"text":[null]}]}]}]}}
        """;

    private const string HomeLineup = """
        {"data":{"regions":[{"rows":[
          {"cells":[{"text":["47"]},{"text":["Verteidigerin"]},{"text":["Melanie Gass"]}]},
          {"cells":[{"text":["1"]},{"text":["Goalie"]},{"text":["Tanja Tor"]}]},
          {"cells":[{"text":["x"]},{"text":[null]},{"text":["Ohne Nummer"]}]}
        ]}]}}
        """;

    private const string AwayLineup = """
        {"data":{"regions":[{"rows":[
          {"cells":[{"text":["41"]},{"text":["Goalie"]},{"text":["Livia Werz"]}]},
          {"cells":[{"text":["3"]},{"text":["Verteidigerin"]},{"text":["Damaris Hangartner"]}]}
        ]}]}}
        """;

    [Fact]
    public void Parses_the_team_game_list()
    {
        var games = SwissUnihockeyJson.ParseGames(Games);
        var game = Assert.Single(games);
        Assert.Equal("1095929", game.Id);
        Assert.Equal(new DateTime(2026, 9, 13, 19, 30, 0), game.Date);
        Assert.Equal("SV Waldenburg Eagles", game.Home);
        Assert.Equal("Red Ants Winterthur", game.Away);
        Assert.Equal("3-Fachhalle, Oberdorf BL", game.Location);
        Assert.Equal("0:21", game.Result);
    }

    [Fact]
    public void Parses_detail_and_lineups_into_a_game()
    {
        var game = SwissUnihockeyJson.ParseGame("1095929", Detail, HomeLineup, AwayLineup);

        Assert.Equal(GameSource.SwissUnihockey, game.Source);
        Assert.Equal("SV Waldenburg Eagles - Red Ants Winterthur", game.Title);
        Assert.Equal("Mobiliar Unihockey Cup Frauen 1/16-Final 2026/27", game.League);
        Assert.Equal(new DateTime(2026, 9, 13, 19, 30, 0), game.Date);
        Assert.Equal("3-Fachhalle", game.Location);
        Assert.Equal("Hans Muster", game.Referee1);
        Assert.Equal("", game.Referee2);

        Assert.Equal(431615, game.Home.TeamId);
        Assert.Equal("https://img/home.png", game.Home.LogoUrl);
        Assert.Equal("SWE", game.Home.ShortName);
        Assert.Equal(2, game.Home.Players.Count);
        Assert.Equal([1, 47], game.Home.Players.Select(p => p.Number));
        Assert.Equal([1], game.Home.Goalies);

        Assert.Equal(428665, game.Away.TeamId);
        Assert.Equal("Livia Werz", game.Away.Find(41)!.Name);
        Assert.Equal([41], game.Away.Goalies);
    }

    [Fact]
    public void Merge_keeps_manual_additions_when_reloading()
    {
        var fresh = SwissUnihockeyJson.ParseGame("1095929", Detail, HomeLineup, AwayLineup);
        var edited = fresh with
        {
            Commentator1 = "Tom",
            Away = fresh.Away with
            {
                Coach = "Coach A",
                Starting6 = [41, 3, 3, 3, 3, 3],
                Players = fresh.Away.Players.Select(p => p.Number == 41 ? p with { Sponsor = "Kempa" } : p).ToList(),
            },
        };

        var merged = edited.Merge(fresh);

        Assert.Equal("Tom", merged.Commentator1);
        Assert.Equal("Coach A", merged.Away.Coach);
        Assert.Equal(6, merged.Away.Starting6.Count);
        Assert.Equal("Kempa", merged.Away.Find(41)!.Sponsor);
    }
}
