using RedAnts.Show.Features;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Agents.Infrastructure;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Games.Infrastructure;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Halls.Infrastructure;
using RedAnts.Show.Features.Scenes;
using RedAnts.Show.Features.Scenes.Infrastructure;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<ShowMigrationComponent>();
        builder.Services.AddSingleton<ShowDatabase>();
        builder.Services.AddSingleton<IHallRepository, HallRepository>();
        builder.Services.AddSingleton<IHallReader, HallReader>();
        builder.Services.AddSingleton<IGameRepository, GameRepository>();
        builder.Services.AddSingleton<IGameReader, GameReader>();
        builder.Services.AddSingleton<ISwissUnihockeyGames, SwissUnihockeyGames>();
        builder.Services.AddSingleton<IScenes, StoredScenes>();
        builder.Services.AddSingleton<IAgentConnections, AgentConnections>();
        builder.Services.AddSingleton<IAgentReports, AgentReports>();
        builder.Services.AddSingleton<IAgentChannel, AgentChannel>();
        builder.Services.AddScoped<AgentSync>();
        builder.Services.AddSingleton<IPackageManifestReader, ShowAdminManifestReader>();
        builder.Services.AddShowFeatures();
    }
}
