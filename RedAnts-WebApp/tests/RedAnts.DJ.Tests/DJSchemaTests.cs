using RedAnts.DJ.Infrastructure;
using Xunit;

namespace RedAnts.DJ.Tests;

public class DJSchemaTests
{
    [Fact]
    public void SchemaNameIsShow() => Assert.Equal("show", DJSchema.SchemaName);
}
