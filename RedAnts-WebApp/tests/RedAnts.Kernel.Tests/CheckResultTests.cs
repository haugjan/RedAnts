using Xunit;
namespace RedAnts.Kernel.Tests;

public class CheckResultTests
{
    private sealed record SoldOut() : CheckResult.Denied.Reason("Ausverkauft.");

    [Fact]
    public void Allow_is_allowed()
    {
        var result = CheckResult.Allow();
        Assert.True(result.IsAllowed);
        Assert.IsType<CheckResult.Allowed>(result);
    }

    [Fact]
    public void Deny_carries_its_reason()
    {
        var result = CheckResult.Deny(new SoldOut());
        Assert.False(result.IsAllowed);
        var denied = Assert.IsType<CheckResult.Denied>(result);
        Assert.Equal("Ausverkauft.", denied.Cause.Message);
        Assert.IsType<SoldOut>(denied.Cause);
    }
}
