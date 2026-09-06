namespace RedAnts.BrowserTests;

public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL")))
            Skip = "Set E2E_BASE_URL (for example http://localhost:5606) to run browser tests against a running app.";
    }
}
