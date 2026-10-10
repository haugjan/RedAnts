using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.DJ.Features.Admin;

public sealed class DJAdminManifestReader : IPackageManifestReader
{
    public const string SectionAlias = "redAnts.show";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.DJAdmin",
            AllowPublicAccess = false,
            Extensions =
            [
                new
                {
                    type = "section",
                    alias = SectionAlias,
                    name = "DJ",
                    weight = 700,
                    meta = new { label = "DJ", pathname = "dj" }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.dj.dashboard",
                    name = "DJ Admin",
                    element = "/_content/RedAnts.DJ/App_Plugins/DJ/dj-view.js",
                    elementName = "dj-admin-view",
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
