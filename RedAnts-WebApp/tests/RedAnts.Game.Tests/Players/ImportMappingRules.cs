using System.Reflection;
using RedAnts.Domain;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players;
using Xunit;

namespace RedAnts.Game.Tests.Players;

public class ImportMappingRules
{
    private const string SeedResource = "RedAnts.Game.Infrastructure.Content.game-players-seed.json";

    private static string Seed()
    {
        var assembly = typeof(GamePlayer).Assembly;
        using var stream = assembly.GetManifestResourceStream(SeedResource);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    [Fact]
    public void The_shipped_seed_fills_a_whole_squad()
    {
        var players = GamePlayerImportMapping.Parse(Seed()).ToPlayers();

        Assert.True(players.Count >= SquadLayout.Slots, "The seed must fill every slot.");
        Assert.True(players.Count(p => p.Position == PlayerPosition.Goalie) >= SquadLayout.Goalies);
        Assert.True(players.Count(p => p.Position == PlayerPosition.Defence)
                    >= SquadLayout.Lines * SquadLayout.DefencePerLine);
        Assert.True(players.Count(p => p.Position == PlayerPosition.Forward)
                    >= SquadLayout.Lines * SquadLayout.ForwardsPerLine);
    }

    [Fact]
    public void The_shipped_seed_carries_portraits_values_and_form()
    {
        var players = GamePlayerImportMapping.Parse(Seed()).ToPlayers();

        Assert.All(players, player =>
        {
            Assert.NotEmpty(player.Name);
            Assert.InRange(player.MarketValue, GamePlayer.MinMarketValue, GamePlayer.MaxMarketValue);
            Assert.NotNull(player.PortraitUrl);
            Assert.NotEmpty(player.FormCsv);
            Assert.Equal("2025/26", player.Season);
        });
    }

    [Fact]
    public void Empty_input_is_refused()
    {
        Assert.Throws<DomainException>(() => GamePlayerImportMapping.Parse(""));
        Assert.Throws<DomainException>(() => GamePlayerImportMapping.Parse("{\"Players\":[]}"));
        Assert.Throws<DomainException>(() => GamePlayerImportMapping.Parse("kein json"));
    }

    [Fact]
    public void An_unknown_position_becomes_a_forward()
    {
        const string json = """
            {"Season":"2025/26","Players":[{"ExternalId":1,"Name":"Test Spielerin","Position":9,
            "MarketValue":40,"Games":10,"Status":77}]}
            """;

        var player = GamePlayerImportMapping.Parse(json).ToPlayers().Single();

        Assert.Equal(PlayerPosition.Forward, player.Position);
        Assert.Equal(PlayerStatus.Unknown, player.Status);
    }

    [Fact]
    public void A_market_value_outside_the_range_is_clamped()
    {
        const string json = """
            {"Players":[{"ExternalId":1,"Name":"Test Spielerin","Position":2,"MarketValue":9000,"Games":1}]}
            """;

        Assert.Equal(GamePlayer.MaxMarketValue, GamePlayerImportMapping.Parse(json).ToPlayers().Single().MarketValue);
    }
}
