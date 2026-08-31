using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace RedAnts.Infrastructure.Show;

public class ShowMigrationComponent(IConfiguration config, IRuntimeState runtimeState, IShowSettingsStore settings) : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < Umbraco.Cms.Core.RuntimeLevel.Run) return;

        var dsn = config.GetConnectionString("showDbDSN");
        if (string.IsNullOrWhiteSpace(dsn)) dsn = config.GetConnectionString("umbracoDbDSN");
        if (string.IsNullOrWhiteSpace(dsn)) return;

        ShowSchema.Ensure(dsn);
        await settings.LoadAsync();
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
