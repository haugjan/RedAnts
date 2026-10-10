using Xunit;

namespace RedAnts.Kernel.Tests;

public class SiteHostsTests
{
    [Theory]
    [InlineData("admin.redants.ch", SiteSurface.Admin)]
    [InlineData("admin-dev.redants.ch", SiteSurface.Admin)]
    [InlineData("scan.redants.ch", SiteSurface.Scan)]
    [InlineData("show-dev.redants.ch", SiteSurface.DJ)]
    [InlineData("dj.redants.ch", SiteSurface.DJ)]
    [InlineData("dj-dev.redants.ch", SiteSurface.DJ)]
    [InlineData("tickets.redants.ch", SiteSurface.Tickets)]
    [InlineData("localhost", SiteSurface.Tickets)]
    public void SurfaceOfHost_maps_every_host(string host, SiteSurface expected) =>
        Assert.Equal(expected, SiteHosts.SurfaceOfHost(host));

    [Theory]
    [InlineData("/umbraco", SiteSurface.Admin)]
    [InlineData("/admin/dj", SiteSurface.Admin)]
    [InlineData("/scan", SiteSurface.Scan)]
    [InlineData("/scanner-test", SiteSurface.Scan)]
    [InlineData("/dj", SiteSurface.DJ)]
    [InlineData("/dj/sound/x.mp3", SiteSurface.DJ)]
    [InlineData("/show", SiteSurface.DJ)]
    [InlineData("/cart", SiteSurface.Tickets)]
    [InlineData("/ticket/abc", SiteSurface.Tickets)]
    public void SurfaceOfPath_maps_surface_paths(string path, SiteSurface expected) =>
        Assert.Equal(expected, SiteHosts.SurfaceOfPath(path));

    [Theory]
    [InlineData("/")]
    [InlineData("/_blazor/negotiate")]
    [InlineData("/_content/RedAnts.DJ/dj/soundboard.js")]
    [InlineData("/api/dj/state")]
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
        Assert.Equal("show.redants.ch", SiteHosts.HostFor(SiteSurface.DJ, dev: false));
        Assert.Equal("show-dev.redants.ch", SiteHosts.HostFor(SiteSurface.DJ, dev: true));
        Assert.Equal("https://admin-dev.redants.ch", SiteHosts.BaseUrlFor(SiteSurface.Admin, dev: true));
    }

    [Fact]
    public void UrlFor_stays_relative_on_the_owning_host_and_on_localhost()
    {
        Assert.Equal("/dj", SiteHosts.UrlFor(SiteSurface.DJ, "show.redants.ch", "/dj"));
        Assert.Equal("/dj", SiteHosts.UrlFor(SiteSurface.DJ, "localhost", "/dj"));
    }

    [Fact]
    public void UrlFor_crosses_to_the_canonical_host_and_keeps_the_stage()
    {
        Assert.Equal("https://show.redants.ch/dj", SiteHosts.UrlFor(SiteSurface.DJ, "admin.redants.ch", "/dj"));
        Assert.Equal("https://show-dev.redants.ch/dj", SiteHosts.UrlFor(SiteSurface.DJ, "admin-dev.redants.ch", "/dj"));
        Assert.Equal("https://scan.redants.ch/scan/DunkleMandel", SiteHosts.UrlFor(SiteSurface.Scan, "admin.redants.ch", "/scan/DunkleMandel"));
        Assert.Equal("https://scan-dev.redants.ch/scan/DunkleMandel", SiteHosts.UrlFor(SiteSurface.Scan, "admin-dev.redants.ch", "/scan/DunkleMandel"));
        Assert.Equal("https://tickets.redants.ch/tickets/event/abc", SiteHosts.UrlFor(SiteSurface.Tickets, "admin.redants.ch", "/tickets/event/abc"));
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
        Assert.Equal("/dj", SiteHosts.RootPathFor(SiteSurface.DJ));
        Assert.Equal("/ticketing/", SiteHosts.RootPathFor(SiteSurface.Tickets));
    }
}
