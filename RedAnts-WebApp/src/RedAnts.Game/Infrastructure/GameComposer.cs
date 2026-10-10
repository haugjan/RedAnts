using RedAnts.Game.Features;
using RedAnts.Game.Features.Admin;
using RedAnts.Game.Features.Admin.Infrastructure;
using RedAnts.Game.Features.Players;
using RedAnts.Game.Features.Players.Infrastructure;
using RedAnts.Game.Features.Round;
using RedAnts.Game.Features.Round.Infrastructure;
using RedAnts.Game.Features.Settings;
using RedAnts.Game.Features.Settings.Infrastructure;
using RedAnts.Game.Features.Squad;
using RedAnts.Game.Features.Standings;
using RedAnts.Game.Features.Standings.Infrastructure;
using RedAnts.Game.Features.Squad.Infrastructure;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Manifest;

namespace RedAnts.Game.Infrastructure;

public sealed class GameComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<GameMigrationComponent>();
        builder.Services.AddSingleton<GameDatabase>();
        builder.Services.AddSingleton<IGameSettings, GameSettingsRepository>();
        builder.Services.AddSingleton<IGamePlayerRepository, GamePlayerRepository>();
        builder.Services.AddSingleton<IGamePlayerReader, GamePlayerReader>();
        builder.Services.AddSingleton<ISquadRepository, SquadRepository>();
        builder.Services.AddSingleton<ISquadReader, SquadReader>();
        builder.Services.AddSingleton<IRoundReader, RoundReader>();
        builder.Services.AddSingleton<IGameOverviewReader, GameOverviewReader>();
        builder.Services.AddSingleton<IStandingsReader, StandingsReader>();
        builder.Services.AddSingleton<IDemoSquads, DemoSquads>();
        builder.Services.AddSingleton<IPackageManifestReader, GameAdminManifestReader>();
        builder.Services.AddGameFeatures();
    }
}
