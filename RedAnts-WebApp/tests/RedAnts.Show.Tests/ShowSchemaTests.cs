using RedAnts.Show.Infrastructure;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowSchemaTests
{
    [Fact]
    public void Creates_every_table_idempotently()
    {
        Assert.Equal("show", ShowSchema.SchemaName);
        foreach (var table in new[] { "Halls", "Games", "AgentReports" })
            Assert.Contains($"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{table}'", ShowSchema.Ddl);
    }
}
