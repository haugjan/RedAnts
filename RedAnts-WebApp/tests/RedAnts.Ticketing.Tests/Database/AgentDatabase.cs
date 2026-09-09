using NPoco;
using System.Data.Common;
using Umbraco.Cms.Infrastructure.Persistence;

namespace RedAnts.Ticketing.Tests.Database;

internal sealed class AgentDatabase(DbConnection connection)
    : NPoco.Database(connection, NPoco.DatabaseType.SqlServer2012), IUmbracoDatabase
{
    public string InstanceId { get; } = Guid.NewGuid().ToString("N")[..8];

    public bool EnableSqlCount { get; set; }

    public int SqlCount => 0;

    public ISqlContext SqlContext => throw new NotSupportedException("The agent database runs without an Umbraco SQL context.");

    public bool IsUmbracoInstalled() => true;

    public bool InTransaction => Transaction is not null;

    public Umbraco.Cms.Infrastructure.Migrations.Install.DatabaseSchemaResult ValidateSchema() => throw new NotSupportedException("The agent database validates no schema.");

    public int BulkInsertRecords<T>(IEnumerable<T> records) => throw new NotSupportedException("The agent database inserts no bulks.");
}
