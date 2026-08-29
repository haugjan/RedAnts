using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;

namespace RedAnts.Infrastructure.Show;

public class ShowMigrationComponent(IConfiguration config, IRuntimeState runtimeState) : IAsyncComponent
{
    public Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < Umbraco.Cms.Core.RuntimeLevel.Run) return Task.CompletedTask;

        var dsn = config.GetConnectionString("showDbDSN");
        if (string.IsNullOrWhiteSpace(dsn)) dsn = config.GetConnectionString("umbracoDbDSN");
        if (string.IsNullOrWhiteSpace(dsn)) return Task.CompletedTask;

        ShowSchema.Ensure(dsn);
        return Task.CompletedTask;
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
