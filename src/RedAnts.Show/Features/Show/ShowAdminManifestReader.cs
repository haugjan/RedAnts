using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Features.Show;

public sealed class ShowAdminManifestReader : IPackageManifestReader
{
    public const string SectionAlias = "redAnts.show";

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
                    weight = 700,
                    meta = new { label = "Show", pathname = "show" }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.show.dashboard",
                    name = "Show Admin",
                    element = "/_content/RedAnts.Show/App_Plugins/Show/show-view.js",
                    elementName = "show-admin-view",
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
