using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace RedAnts.Show.Infrastructure;

public class ShowMigrationComponent(IConfiguration config, IRuntimeState runtimeState, ShowDatabase database) : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < Umbraco.Cms.Core.RuntimeLevel.Run) return;
        var migrationsEnabled = config.GetValue<bool>("Migrations:RunAtBoot") || config.GetValue<bool>("Migrations:RunNow");
        if (migrationsEnabled) await ShowSchema.EnsureAsync(database);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
