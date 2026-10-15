using Microsoft.Data.SqlClient;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowDatabase(IScopeProvider scopeProvider, IConfiguration config)
{
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    private string? SeparateDsn =>
        config.GetConnectionString("showDbDSN") is { Length: > 0 } dsn ? dsn : null;

    public async Task<T> RunAsync<T>(Func<IDatabase, Task<T>> work)
    {
        await OneAtATime.WaitAsync();
        try
        {
            if (SeparateDsn is { } dsn)
            {
                using var database = new Database(dsn, DatabaseType.SqlServer2012, SqlClientFactory.Instance);
                using var transaction = database.GetTransaction();
                var separate = await work(database);
                transaction.Complete();
                return separate;
            }

            using var scope = scopeProvider.CreateScope();
            var shared = await work(scope.Database);
            scope.Complete();
            return shared;
        }
        finally { OneAtATime.Release(); }
    }

    public Task RunAsync(Func<IDatabase, Task> work) => RunAsync(async db =>
    {
        await work(db);
        return true;
    });
}
