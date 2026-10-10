using RedAnts.DJ.Features;
using RedAnts.DJ.Features.Admin;
using RedAnts.DJ.Features.Board;
using RedAnts.DJ.Features.Remote;
using RedAnts.DJ.Features.Remote.Infrastructure;
using RedAnts.DJ.Features.Sounds;
using RedAnts.DJ.Features.Sounds.Infrastructure;
using QuestPDF.Infrastructure;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.DJ.Infrastructure;

public sealed class DJComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        builder.AddComponent<DJMigrationComponent>();
        builder.Services.AddSingleton<DJDatabase>();
        builder.Services.AddSingleton<IDJProfiles, DJProfileRepository>();
        builder.Services.AddSingleton<IDJBoardPdf, DJBoardPdfRenderer>();
        builder.Services.AddSingleton<IDJSettings, DJSettingsRepository>();
        builder.Services.AddSingleton<IDJRemote, DJRemote>();
        builder.Services.AddSingleton<IDJBoardViews, DJBoardViews>();
        builder.Services.AddSingleton<IDJRooms, DJRooms>();
        builder.Services.AddSingleton<IDJSoundUploader, DJSoundUploader>();
        builder.Services.AddSingleton<DJSpotifyAccount>();
        builder.Services.AddSingleton<IDJSpotifyAccount>(sp => sp.GetRequiredService<DJSpotifyAccount>());
        builder.Services.AddSingleton<IDJSpotifySearch, DJSpotifySearch>();
        builder.Services.AddSingleton<IPackageManifestReader, DJAdminManifestReader>();
        builder.Services.AddDJFeatures();
    }
}
