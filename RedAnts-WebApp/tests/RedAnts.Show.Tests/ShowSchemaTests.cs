using RedAnts.Infrastructure.Show;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowSchemaTests
{
    [Fact]
    public void SchemaNameIsShow() => Assert.Equal("show", ShowSchema.SchemaName);
}
