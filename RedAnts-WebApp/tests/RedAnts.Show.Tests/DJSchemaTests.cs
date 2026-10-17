using RedAnts.Show.Infrastructure;
using Xunit;

namespace RedAnts.Show.Tests;

public class DJSchemaTests
{
    [Fact]
    public void SchemaNameIsShow() => Assert.Equal("show", DJSchema.SchemaName);
}
