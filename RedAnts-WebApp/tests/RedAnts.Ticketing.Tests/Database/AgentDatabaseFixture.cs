using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NPoco;
using System.Data.Common;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Xunit;

namespace RedAnts.Ticketing.Tests.Database;

public sealed class AgentDatabaseFixture : IAsyncLifetime
{
    private DbConnection? _connection;
    private AgentDatabase? _database;
    private AgentScopeProvider? _scopes;

    public static string? ConnectionString { get; } = Resolve();

    public static bool Available => !string.IsNullOrWhiteSpace(ConnectionString);

    public IScopeProvider Scopes => _scopes ?? throw new InvalidOperationException("The fixture is not initialised.");

    public IDatabase Database => _database ?? throw new InvalidOperationException("The fixture is not initialised.");

    public int UnusedId { get; } = Random.Shared.Next(900_000, 999_999);

    public async Task InitializeAsync()
    {
        var connection = new SqlConnection(WithCredentialHeadroom(ConnectionString
            ?? throw new InvalidOperationException("No agent database is configured.")));
        await connection.OpenAsync();
        _connection = connection;
        _database = new AgentDatabase(connection);
        _database.BeginTransaction();
        _scopes = new AgentScopeProvider(_database);
    }

    public async Task DisposeAsync()
    {
        _database?.AbortTransaction();
        _database?.Dispose();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    private static string? Resolve()
    {
        var fromEnvironment = Clean(Environment.GetEnvironmentVariable("AGENT_DSN"));
        if (fromEnvironment is not null) return fromEnvironment;

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AgentDatabaseFixture>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        return Clean(configuration["RedAnts:AgentDsn"]);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string WithCredentialHeadroom(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (builder.ConnectTimeout < 60) builder.ConnectTimeout = 60;
        return builder.ConnectionString;
    }
}

[CollectionDefinition(AgentDatabaseCollection.Name, DisableParallelization = true)]
public sealed class AgentDatabaseCollection
{
    public const string Name = "agent database";
}
