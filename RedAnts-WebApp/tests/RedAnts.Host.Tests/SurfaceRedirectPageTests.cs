using RedAnts.Infrastructure.Shared;
using Xunit;

namespace RedAnts.Host.Tests;

public class SurfaceRedirectPageTests
{
    [Fact]
    public void Html_forwards_to_the_target_via_meta_refresh_and_button()
    {
        var target = "https://scan.redants.ch/scan/DunkleMandel";
        var html = SurfaceRedirectPage.Html(target, "scan.redants.ch");

        Assert.Contains($"content=\"5;url={target}\"", html);
        Assert.Contains($"href=\"{target}\"", html);
        Assert.Contains("scan.redants.ch", html);
    }

    [Fact]
    public void Html_encodes_an_attacker_controlled_path()
    {
        var target = "https://scan.redants.ch/scan/\"><script>alert(1)</script>";
        var html = SurfaceRedirectPage.Html(target, "scan.redants.ch");

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
