using RedAnts.Show.Features.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace RedAnts.Show.Infrastructure;

public class ShowMigrationComponent(IConfiguration config, IRuntimeState runtimeState, IShowSettings settings, ShowDatabase database)
    : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < Umbraco.Cms.Core.RuntimeLevel.Run) return;
        var migrationsEnabled = config.GetValue<bool>("Migrations:RunAtBoot") || config.GetValue<bool>("Migrations:RunNow");
        if (migrationsEnabled) await ShowSchema.EnsureAsync(database);
        await settings.LoadAsync();
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
