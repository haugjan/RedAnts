using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Show.Features.Admin;

public sealed class ShowAdminManifestReader : IPackageManifestReader
{
    public const string SectionAlias = "redAnts.halle";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.ShowAdmin",
            AllowPublicAccess = false,
            Extensions =
            [
                new
                {
                    type = "section",
                    alias = SectionAlias,
                    name = "Show",
                    weight = 690,
                    meta = new { label = "Show", pathname = "show" }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.halle.hallen",
                    name = "Hallen",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 1000,
                    meta = new { label = "Hallen", pathname = "hallen" },
                    conditions = new object[]
                    {
                        new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                    }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.halle.musik",
                    name = "Musik",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 900,
                    meta = new { label = "Musik", pathname = "musik" },
                    conditions = new object[]
                    {
                        new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                    }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.halle.decks",
                    name = "Decks",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 800,
                    meta = new { label = "Decks", pathname = "decks" },
                    conditions = new object[]
                    {
                        new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                    }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.halle.bedienung",
                    name = "Bedienung",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 700,
                    meta = new { label = "Bedienung", pathname = "bedienung" },
                    conditions = new object[]
                    {
                        new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                    }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.halle.vmix",
                    name = "vMix-Titel",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 600,
                    meta = new { label = "vMix-Titel", pathname = "vmix" },
                    conditions = new object[]
                    {
                        new { alias = "Umb.Condition.SectionAlias", match = SectionAlias }
                    }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.halle.led",
                    name = "LED-Vorlagen",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 500,
                    meta = new { label = "LED-Vorlagen", pathname = "led" },
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
