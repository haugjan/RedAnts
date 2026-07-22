using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace RedAnts.Features.Website;

public sealed class SeoController(UmbracoHelper umbraco, IUmbracoContextFactory contextFactory) : Controller
{
    private static bool IsIndexableHost(string host) =>
        host.Equals("tickets.redants.ch", StringComparison.OrdinalIgnoreCase);

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var sb = new StringBuilder();
        sb.Append("User-agent: *\n");
        if (IsIndexableHost(Request.Host.Host))
        {
            sb.Append("Disallow: /umbraco\n");
            sb.Append("Disallow: /checkout\n");
            sb.Append("Disallow: /cart\n");
            sb.Append("Disallow: /scan\n");
            sb.Append("Disallow: /ticket/\n");
            sb.Append($"Sitemap: {Request.Scheme}://{Request.Host}/sitemap.xml\n");
        }
        else
        {
            sb.Append("Disallow: /\n");
        }
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    public IActionResult Sitemap()
    {
        if (!IsIndexableHost(Request.Host.Host)) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var urls = new List<string>();
        using (contextFactory.EnsureUmbracoContext())
        {
            foreach (var root in umbraco.ContentAtRoot())
                Collect(root, baseUrl, urls);
        }
        urls.Add($"{baseUrl}/seasons/");

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var doc = new XDocument(new XElement(ns + "urlset",
            urls.Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(u => new XElement(ns + "url", new XElement(ns + "loc", u)))));
        return Content("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + doc, "application/xml; charset=utf-8");
    }

    private static void Collect(IPublishedContent node, string baseUrl, List<string> urls)
    {
        if (node.IsPublished())
        {
            if (node.ContentType.Alias == "flexPage")
            {
                var url = node.Url(mode: UrlMode.Absolute);
                if (!string.IsNullOrEmpty(url)) urls.Add(url);
            }
            else if (node.ContentType.Alias == "legalPage")
            {
                var slug = node.Value<string>("slug");
                if (!string.IsNullOrWhiteSpace(slug)) urls.Add($"{baseUrl}/{slug}");
            }
        }
        foreach (var child in node.Children())
            Collect(child, baseUrl, urls);
    }
}
