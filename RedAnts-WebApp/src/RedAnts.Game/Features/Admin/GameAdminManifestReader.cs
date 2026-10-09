using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Game.Features.Admin;

public sealed class GameAdminManifestReader : IPackageManifestReader
{
    public const string SectionAlias = "redAnts.game";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.GameAdmin",
            AllowPublicAccess = false,
            Extensions =
            [
                new
                {
                    type = "section",
                    alias = SectionAlias,
                    name = "Game",
                    weight = 600,
                    meta = new { label = "Game", pathname = "game" }
                },
                new
                {
                    type = "dashboard",
                    alias = "redAnts.game.dashboard",
                    name = "Game Admin",
                    element = "/_content/RedAnts.Game/App_Plugins/Game/game-view.js",
                    elementName = "game-admin-view",
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
