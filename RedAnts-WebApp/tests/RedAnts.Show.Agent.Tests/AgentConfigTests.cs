namespace RedAnts.Show.Agent.Tests;

public class AgentConfigTests
{
    [Fact]
    public void Without_a_bind_the_board_stays_on_localhost()
    {
        var config = new AgentConfig("https://show.redants.ch", "win4", "key");

        Assert.Equal("localhost", config.Host);
        Assert.False(config.OnNetwork);
        Assert.Null(config.Warning);
    }

    [Fact]
    public void A_board_on_the_network_without_a_password_falls_back_to_localhost()
    {
        var config = new AgentConfig("https://show.redants.ch", "win4", "key", Bind: "0.0.0.0");

        Assert.Equal("localhost", config.Host);
        Assert.False(config.OnNetwork);
        Assert.Contains("boardPassword", config.Warning);
    }

    [Fact]
    public void A_board_on_the_network_with_a_password_listens_where_it_was_told()
    {
        var config = new AgentConfig("https://show.redants.ch", "win4", "key", Bind: "0.0.0.0", BoardPassword: "geheim");

        Assert.Equal("0.0.0.0", config.Host);
        Assert.True(config.OnNetwork);
        Assert.Null(config.Warning);
    }
}
