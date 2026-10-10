using RedAnts.DJ.Features.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace RedAnts.DJ.Infrastructure;

public class DJMigrationComponent(IConfiguration config, IRuntimeState runtimeState, IDJSettings settings, DJDatabase database)
    : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < Umbraco.Cms.Core.RuntimeLevel.Run) return;
        var migrationsEnabled = config.GetValue<bool>("Migrations:RunAtBoot") || config.GetValue<bool>("Migrations:RunNow");
        if (migrationsEnabled) await DJSchema.EnsureAsync(database);
        await settings.LoadAsync();
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
