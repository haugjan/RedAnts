using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>
/// Registers a backoffice <b>section</b> "Ticketing" with one <b>dashboard per admin tab</b>. Each
/// dashboard element is a web component that embeds the Blazor admin app in an iframe, preselecting
/// its tab. Using native dashboards (instead of a single dashboard + in-app tab state) means Umbraco
/// owns the routing, so every tab is deep-linkable and switching a tab rewrites the browser URL to
/// <c>/umbraco/section/ticketing/dashboard/{pathname}</c>.
/// Pattern mirrored from the Sporthalle Sulzerallee project (section + dashboard + iframe + Blazor).
/// </summary>
public sealed class TicketingAdminManifestReader : IPackageManifestReader
{
    public const string SectionAlias = "redAnts.ticketing";

    private const string ElementFile = "/App_Plugins/TicketingAdmin/ticketing-admin-view.js";

    /// <summary>
    /// The admin tabs, in display order. <c>Pathname</c> is both the URL segment
    /// (<c>/umbraco/section/ticketing/dashboard/{pathname}</c>) and the <c>?tab=</c> value passed to
    /// the iframe; <c>ElementName</c> is the custom element defined in <see cref="ElementFile"/>.
    /// </summary>
    private static readonly (string Pathname, string Label, string ElementName)[] Tabs =
    [
        ("events", "Anlässe", "ticketing-admin-events"),
        ("tickets", "Tickets", "ticketing-admin-tickets"),
        ("seasoncards", "Saisonkarten", "ticketing-admin-seasoncards"),
        ("membercards", "Mitgliederkarten", "ticketing-admin-membercards")
    ];

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var extensions = new List<object>
        {
            // Left-hand backoffice section (the "menu point").
            new
            {
                type = "section",
                alias = SectionAlias,
                name = "Ticketing",
                weight = 800,
                meta = new { label = "Ticketing", pathname = "ticketing" }
            }
        };

        // One dashboard per tab. They render as the section's tab bar; higher weight sorts first, so
        // the list order above becomes left-to-right and the first tab is the section's default view.
        for (var i = 0; i < Tabs.Length; i++)
        {
            var (pathname, label, elementName) = Tabs[i];
            extensions.Add(new
            {
                type = "dashboard",
                alias = $"redAnts.ticketing.dashboard.{pathname}",
                name = $"Ticketing Admin – {label}",
                element = ElementFile,
                elementName,
                weight = 100 * (Tabs.Length - i),
                meta = new { label, pathname },
                conditions = new object[]
                {
                    new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                }
            });
        }

        var manifest = new PackageManifest
        {
            Name = "RedAnts.TicketingAdmin",
            AllowPublicAccess = false,
            Extensions = extensions.ToArray()
        };

        return Task.FromResult<IEnumerable<PackageManifest>>(new[] { manifest });
    }
}

/// <summary>Wires the manifest reader into Umbraco (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class TicketingAdminComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<IPackageManifestReader, TicketingAdminManifestReader>();
}
