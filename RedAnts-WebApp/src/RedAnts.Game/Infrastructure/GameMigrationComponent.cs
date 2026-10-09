using RedAnts.Game.Features.Players;
using RedAnts.Game.Features.Settings;
using RedAnts.Game.Infrastructure.Content;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace RedAnts.Game.Infrastructure;

public class GameMigrationComponent(
    IConfiguration config,
    IRuntimeState runtimeState,
    IGameSettings settings,
    IGamePlayerRepository players,
    GameDatabase database,
    ILogger<GameMigrationComponent> logger) : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < Umbraco.Cms.Core.RuntimeLevel.Run) return;

        var migrationsEnabled = config.GetValue<bool>("Migrations:RunAtBoot") || config.GetValue<bool>("Migrations:RunNow");
        if (migrationsEnabled)
        {
            await GameSchema.EnsureAsync(database);
            await settings.LoadAsync();
            await GamePlayerSeeding.EnsureAsync(players, settings, logger);
            return;
        }

        await settings.LoadAsync();
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
