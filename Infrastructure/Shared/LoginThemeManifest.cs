using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Infrastructure.Shared;

public sealed class LoginThemeManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.LoginTheme",
            AllowPublicAccess = true,
            Extensions =
            [
                new
                {
                    type = "appEntryPoint",
                    alias = "RedAnts.Login.Theme",
                    name = "Red Ants Login Theme",
                    js = "/App_Plugins/RedAntsLogin/login-theme.js"
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>(new[] { manifest });
    }
}

public sealed class LoginThemeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, LoginThemeManifestReader>();
    }
}
