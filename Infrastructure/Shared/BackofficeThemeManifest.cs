using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Infrastructure.Shared;

public sealed class BackofficeThemeManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.BackofficeTheme",
            AllowPublicAccess = false,
            Extensions =
            [
                new
                {
                    type = "appEntryPoint",
                    alias = "RedAnts.Backoffice.Theme",
                    name = "Red Ants Backoffice Theme",
                    js = "/App_Plugins/RedAntsLogin/backoffice-theme.js"
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>(new[] { manifest });
    }
}

public sealed class BackofficeThemeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, BackofficeThemeManifestReader>();
    }
}
