using System.Text.Json;
using RedAnts.Show.Agent.Web;

namespace RedAnts.Show.Agent.Tests;

public class CompanionDownloadTests
{
    [Fact]
    public void Takes_the_newest_published_companion_package()
    {
        using var json = JsonDocument.Parse("""
            [
              { "tag_name": "show-agent-v1.0.65", "draft": false, "prerelease": false,
                "assets": [ { "name": "RedAnts.Show.Agent-win-x64.zip", "browser_download_url": "https://x/agent.zip" } ] },
              { "tag_name": "halle-companion-v1.0.8", "draft": true, "prerelease": false,
                "assets": [ { "name": "redants-halle-1.0.8.tgz", "browser_download_url": "https://x/draft.tgz" } ] },
              { "tag_name": "halle-companion-v1.0.7", "draft": false, "prerelease": false,
                "assets": [ { "name": "notes.txt", "browser_download_url": "https://x/notes.txt" },
                            { "name": "redants-halle-1.0.7.tgz", "browser_download_url": "https://x/1.0.7.tgz" } ] },
              { "tag_name": "halle-companion-v1.0.6", "draft": false, "prerelease": false,
                "assets": [ { "name": "redants-halle-1.0.6.tgz", "browser_download_url": "https://x/1.0.6.tgz" } ] }
            ]
            """);

        Assert.Equal("https://x/1.0.7.tgz", CompanionDownload.Pick(json.RootElement));
    }

    [Fact]
    public void Without_a_companion_release_there_is_no_package()
    {
        using var json = JsonDocument.Parse("""
            [ { "tag_name": "show-agent-v1.0.65", "assets": [ { "name": "a.zip", "browser_download_url": "https://x/a.zip" } ] } ]
            """);

        Assert.Null(CompanionDownload.Pick(json.RootElement));
    }
}
