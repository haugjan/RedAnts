using Xunit;

namespace RedAnts.Kernel.Tests;

public class SiteHostsTests
{
    [Theory]
    [InlineData("admin.redants.ch", SiteSurface.Admin)]
    [InlineData("admin-dev.redants.ch", SiteSurface.Admin)]
    [InlineData("scan.redants.ch", SiteSurface.Scan)]
    [InlineData("show-dev.redants.ch", SiteSurface.Show)]
    [InlineData("tickets.redants.ch", SiteSurface.Tickets)]
    [InlineData("localhost", SiteSurface.Tickets)]
    public void SurfaceOfHost_maps_every_host(string host, SiteSurface expected) =>
        Assert.Equal(expected, SiteHosts.SurfaceOfHost(host));

    [Theory]
    [InlineData("/umbraco", SiteSurface.Admin)]
    [InlineData("/admin/show", SiteSurface.Admin)]
    [InlineData("/scan", SiteSurface.Scan)]
    [InlineData("/scanner-test", SiteSurface.Scan)]
    [InlineData("/show", SiteSurface.Show)]
    [InlineData("/show/sound/x.mp3", SiteSurface.Show)]
    [InlineData("/cart", SiteSurface.Tickets)]
    [InlineData("/ticket/abc", SiteSurface.Tickets)]
    public void SurfaceOfPath_maps_surface_paths(string path, SiteSurface expected) =>
        Assert.Equal(expected, SiteHosts.SurfaceOfPath(path));

    [Theory]
    [InlineData("/")]
    [InlineData("/_blazor/negotiate")]
    [InlineData("/_content/RedAnts.Show/show/soundboard.js")]
    [InlineData("/api/show/state")]
    [InlineData("/health")]
    [InlineData("/media/1/logo.png")]
    [InlineData("/__gate")]
    [InlineData("/robots.txt")]
    [InlineData("/favicon.ico")]
    public void SurfaceOfPath_leaves_shared_paths_on_every_host(string path) =>
        Assert.Null(SiteHosts.SurfaceOfPath(path));

    [Fact]
    public void SurfaceOfPath_matches_whole_segments_only() =>
        Assert.Equal(SiteSurface.Tickets, SiteHosts.SurfaceOfPath("/administration"));

    [Fact]
    public void HostFor_switches_between_prod_and_dev()
    {
        Assert.Equal("show.redants.ch", SiteHosts.HostFor(SiteSurface.Show, dev: false));
        Assert.Equal("show-dev.redants.ch", SiteHosts.HostFor(SiteSurface.Show, dev: true));
        Assert.Equal("https://admin-dev.redants.ch", SiteHosts.BaseUrlFor(SiteSurface.Admin, dev: true));
    }

    [Fact]
    public void UrlFor_stays_relative_on_the_owning_host_and_on_localhost()
    {
        Assert.Equal("/show", SiteHosts.UrlFor(SiteSurface.Show, "show.redants.ch", "/show"));
        Assert.Equal("/show", SiteHosts.UrlFor(SiteSurface.Show, "localhost", "/show"));
    }

    [Fact]
    public void UrlFor_crosses_to_the_canonical_host_and_keeps_the_stage()
    {
        Assert.Equal("https://show.redants.ch/show", SiteHosts.UrlFor(SiteSurface.Show, "admin.redants.ch", "/show"));
        Assert.Equal("https://show-dev.redants.ch/show", SiteHosts.UrlFor(SiteSurface.Show, "admin-dev.redants.ch", "/show"));
    }

    [Fact]
    public void Only_the_public_tickets_host_is_indexable()
    {
        Assert.True(SiteHosts.IsIndexableHost("tickets.redants.ch"));
        Assert.False(SiteHosts.IsIndexableHost("tickets-dev.redants.ch"));
        Assert.False(SiteHosts.IsIndexableHost("admin.redants.ch"));
    }

    [Fact]
    public void RootPathFor_sends_every_host_to_its_own_surface()
    {
        Assert.Equal("/umbraco", SiteHosts.RootPathFor(SiteSurface.Admin));
        Assert.Equal("/scan", SiteHosts.RootPathFor(SiteSurface.Scan));
        Assert.Equal("/show", SiteHosts.RootPathFor(SiteSurface.Show));
        Assert.Equal("/ticketing/", SiteHosts.RootPathFor(SiteSurface.Tickets));
    }
}
