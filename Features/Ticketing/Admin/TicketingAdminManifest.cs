using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>
/// Registers a backoffice <b>section</b> "Ticketing" with a single <b>dashboard</b> whose element is a
/// web component (<c>ticketing-admin-view</c>) that embeds the Blazor admin app in an iframe.
/// Pattern mirrored from the Sporthalle Sulzerallee project (section + dashboard + iframe + Blazor).
/// </summary>
public sealed class TicketingAdminManifestReader : IPackageManifestReader
{
    public const string SectionAlias = "redAnts.ticketing";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.TicketingAdmin",
            AllowPublicAccess = false,
            Extensions =
            [
                // Left-hand backoffice section (the "menu point").
                new
                {
                    type = "section",
                    alias = SectionAlias,
                    name = "Ticketing",
                    weight = 800,
                    meta = new { label = "Ticketing", pathname = "ticketing" }
                },
                // Single dashboard inside that section: renders the iframe web component.
                new
                {
                    type = "dashboard",
                    alias = "redAnts.ticketing.dashboard",
                    name = "Ticketing Admin",
                    element = "/App_Plugins/TicketingAdmin/ticketing-admin-view.js",
                    elementName = "ticketing-admin-view",
                    weight = 100,
                    meta = new { label = "Übersicht", pathname = "overview" },
                    conditions = new object[]
                    {
                        new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                    }
                }
            ]
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
