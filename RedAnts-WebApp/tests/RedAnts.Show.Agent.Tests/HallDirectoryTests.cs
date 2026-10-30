using RedAnts.Show.Agent.Web;

namespace RedAnts.Show.Agent.Tests;

public class HallDirectoryTests
{
    [Fact]
    public void The_cloud_list_becomes_hall_choices()
    {
        var halls = HallDirectory.ParseHalls("""
            [ { "id": "win4", "name": "WIN4 Stratos", "agent": false },
              { "id": "obs", "name": "", "agent": true },
              { "id": "", "name": "ohne Kennung" } ]
            """);

        Assert.NotNull(halls);
        Assert.Equal(2, halls.Count);
        Assert.Equal(new HallChoice("win4", "WIN4 Stratos", false), halls[0]);
        Assert.Equal(new HallChoice("obs", "obs", true), halls[1]);
    }

    [Fact]
    public void A_login_page_instead_of_the_list_counts_as_refused()
    {
        Assert.Null(HallDirectory.ParseHalls("<!DOCTYPE html><html><body>Passwort</body></html>"));
    }

    [Fact]
    public void The_agent_access_carries_the_key()
    {
        var access = HallDirectory.ParseAccess("""{ "id": "win4", "name": "WIN4 Stratos", "key": "abc" }""");

        Assert.Equal(new HallAccess("win4", "WIN4 Stratos", "abc"), access);
    }

    [Fact]
    public void An_access_without_key_is_no_access()
    {
        Assert.Null(HallDirectory.ParseAccess("""{ "id": "win4", "name": "WIN4 Stratos", "key": "" }"""));
    }
}
