namespace RedAnts.Domain;

public enum SiteSurface
{
    Tickets,
    Admin,
    Scan,
    Show,
}

public static class SiteHosts
{
    public const string RootDomain = "redants.ch";

    public static bool IsSiteHost(string? host) =>
        host is not null && host.EndsWith("." + RootDomain, StringComparison.OrdinalIgnoreCase);

    public static bool IsDevHost(string? host) =>
        host is not null && host.Contains("-dev.", StringComparison.OrdinalIgnoreCase);

    public static string Prefix(SiteSurface surface) => surface switch
    {
        SiteSurface.Admin => "admin",
        SiteSurface.Scan => "scan",
        SiteSurface.Show => "show",
        _ => "tickets",
    };

    public static string HostFor(SiteSurface surface, bool dev) =>
        $"{Prefix(surface)}{(dev ? "-dev" : "")}.{RootDomain}";

    public static string BaseUrlFor(SiteSurface surface, bool dev) =>
        "https://" + HostFor(surface, dev);

    public static SiteSurface SurfaceOfHost(string? host)
    {
        if (host is null) return SiteSurface.Tickets;
        foreach (var surface in new[] { SiteSurface.Admin, SiteSurface.Scan, SiteSurface.Show })
        {
            var prefix = Prefix(surface);
            if (host.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith(prefix + "-dev.", StringComparison.OrdinalIgnoreCase))
                return surface;
        }
        return SiteSurface.Tickets;
    }

    public static bool HostServes(string? host, SiteSurface surface) =>
        SurfaceOfHost(host) == surface;

    public static bool IsIndexableHost(string? host) =>
        host is not null && host.Equals(HostFor(SiteSurface.Tickets, false), StringComparison.OrdinalIgnoreCase);

    public static string RootPathFor(SiteSurface surface) => surface switch
    {
        SiteSurface.Admin => "/umbraco",
        SiteSurface.Scan => "/scan",
        SiteSurface.Show => "/show",
        _ => "/ticketing/",
    };

    public static SiteSurface? SurfaceOfPath(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return null;

        foreach (var shared in SharedPrefixes)
            if (StartsWithSegment(path, shared)) return null;

        if (StartsWithSegment(path, "/umbraco")
            || StartsWithSegment(path, "/umbraco-entra-signin")
            || StartsWithSegment(path, "/umbraco-entra-signout")
            || StartsWithSegment(path, "/admin"))
            return SiteSurface.Admin;

        if (StartsWithSegment(path, "/scan") || StartsWithSegment(path, "/scanner-test"))
            return SiteSurface.Scan;

        if (StartsWithSegment(path, "/show"))
            return SiteSurface.Show;

        return HasExtension(path) ? null : SiteSurface.Tickets;
    }

    public static string UrlFor(SiteSurface surface, string? currentHost, string path)
    {
        if (!IsSiteHost(currentHost) || HostServes(currentHost, surface)) return path;
        return BaseUrlFor(surface, IsDevHost(currentHost)) + path;
    }

    private static readonly string[] SharedPrefixes =
    [
        "/_blazor", "/_framework", "/_content", "/App_Plugins", "/api", "/health", "/warmup",
        "/css", "/js", "/img", "/lib", "/media", "/favicons", "/icons", "/__gate",
    ];

    private static bool StartsWithSegment(string path, string prefix) =>
        path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        && (path.Length == prefix.Length || path[prefix.Length] == '/');

    private static bool HasExtension(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        var dot = path.LastIndexOf('.');
        return dot > lastSlash + 1 && dot < path.Length - 1;
    }
}
