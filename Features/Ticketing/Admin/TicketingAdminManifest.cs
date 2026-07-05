using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Features.Ticketing.Admin;

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
                new
                {
                    type = "section",
                    alias = SectionAlias,
                    name = "Ticketing",
                    weight = 800,
                    meta = new { label = "Ticketing", pathname = "ticketing" }
                },
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

public sealed class TicketingAdminComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, TicketingAdminManifestReader>();
        builder.Services.AddScoped<TicketingAdminState>();
    }
}
