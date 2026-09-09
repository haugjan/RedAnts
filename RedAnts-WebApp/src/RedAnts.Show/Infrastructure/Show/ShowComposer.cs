using RedAnts.Features.Show;
using RedAnts.Features.Show.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Infrastructure.Show;

public sealed class ShowComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<ShowMigrationComponent>();
        builder.Services.AddSingleton<ShowDatabase>();
        builder.Services.AddSingleton<IShowProfiles, ShowProfileRepository>();
        builder.Services.AddSingleton<IShowSettings, ShowSettingsRepository>();
        builder.Services.AddSingleton<IShowRemote, ShowRemote>();
        builder.Services.AddSingleton<IShowSoundUploader, ShowSoundUploader>();
        builder.Services.AddSingleton<ShowSpotifyAccount>();
        builder.Services.AddSingleton<IShowSpotifyAccount>(sp => sp.GetRequiredService<ShowSpotifyAccount>());
        builder.Services.AddSingleton<IShowSpotifySearch, ShowSpotifySearch>();
        builder.Services.AddSingleton<IPackageManifestReader, ShowAdminManifestReader>();
        builder.Services.AddShowFeatures();
    }
}
