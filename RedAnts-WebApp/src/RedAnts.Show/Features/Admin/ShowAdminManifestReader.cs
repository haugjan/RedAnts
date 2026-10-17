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
                    alias = "redAnts.halle.dashboard",
                    name = "Show Admin",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 200,
                    meta = new { label = "Halle", pathname = "halle" },
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
                    element = "/_content/RedAnts.Show/App_Plugins/Show/music-view.js",
                    elementName = "music-admin-view",
                    weight = 100,
                    meta = new { label = "Musik", pathname = "musik" },
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
