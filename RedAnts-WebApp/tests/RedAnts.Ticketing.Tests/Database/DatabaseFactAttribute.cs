using Xunit;

namespace RedAnts.Ticketing.Tests.Database;

public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        if (!AgentDatabaseFixture.Available)
            Skip = "No agent database configured (user secret RedAnts:AgentDsn or environment variable AGENT_DSN).";
    }
}
