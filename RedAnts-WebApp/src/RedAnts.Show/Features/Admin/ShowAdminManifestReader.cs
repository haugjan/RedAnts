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
                    alias = "redAnts.halle.show",
                    name = "Show",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
                    weight = 1000,
                    meta = new { label = "Übersicht", pathname = "uebersicht" },
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
