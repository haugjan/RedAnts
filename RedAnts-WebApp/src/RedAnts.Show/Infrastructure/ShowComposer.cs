using RedAnts.Show.Features;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Agents.Infrastructure;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Board.Infrastructure;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Games.Infrastructure;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Halls.Infrastructure;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Music.Infrastructure;
using RedAnts.Show.Features.Remote;
using RedAnts.Show.Features.Remote.Infrastructure;
using RedAnts.Show.Features.Sounds;
using RedAnts.Show.Features.Sounds.Infrastructure;
using QuestPDF.Infrastructure;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        builder.AddComponent<ShowMigrationComponent>();
        builder.AddComponent<DJMigrationComponent>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, Content.ShowExampleSeeder>();
        builder.Services.AddSingleton<ShowDatabase>();
        builder.Services.AddSingleton<IHallRepository, HallRepository>();
        builder.Services.AddSingleton<IHallReader, HallReader>();
        builder.Services.AddSingleton<IGameRepository, GameRepository>();
        builder.Services.AddSingleton<IGameReader, GameReader>();
        builder.Services.AddSingleton<ISwissUnihockeyGames, SwissUnihockeyGames>();
        builder.Services.AddScoped<IMusicLibrary, DJMusicLibrary>();
        builder.Services.AddSingleton<IAgentConnections, AgentConnections>();
        builder.Services.AddSingleton<IAgentReports, AgentReports>();
        builder.Services.AddSingleton<IAgentChannel, AgentChannel>();
        builder.Services.AddSingleton<ShowBoardFeed>();
        builder.Services.AddSingleton<ShowBoardMusic>();
        builder.Services.AddSingleton<IDeckMirror, AgentDeckMirror>();
        builder.Services.AddScoped<AgentSync>();
        builder.Services.AddSingleton<IPackageManifestReader, ShowAdminManifestReader>();
        builder.Services.AddScoped<ShowAdminState>();
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
        builder.Services.AddShowFeatures();
    }
}
