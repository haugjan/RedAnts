using RedAnts.Show.Features;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Remote;
using RedAnts.Show.Features.Remote.Infrastructure;
using RedAnts.Show.Features.Sounds;
using RedAnts.Show.Features.Sounds.Infrastructure;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Show.Infrastructure;

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
