using RedAnts.Features.Show;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Infrastructure.Show;

public sealed class ShowComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<ShowMigrationComponent>();
        builder.Services.AddSingleton<IShowProfileStore, ShowProfileStore>();
        builder.Services.AddSingleton<IShowSettingsStore, ShowSettingsStore>();
        builder.Services.AddSingleton<IShowSoundUploader, ShowSoundUploader>();
        builder.Services.AddSingleton<IShowSpotifySearch, ShowSpotifySearch>();
        builder.Services.AddSingleton<IPackageManifestReader, ShowAdminManifestReader>();
    }
}
