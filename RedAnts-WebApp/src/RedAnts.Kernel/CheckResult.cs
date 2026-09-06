namespace RedAnts.Domain;

public abstract record CheckResult
{
    public sealed record Allowed : CheckResult;

    public sealed record Denied(Denied.Reason Cause) : CheckResult
    {
        public abstract record Reason(string Message);
    }

    public bool IsAllowed => this is Allowed;

    public static CheckResult Allow() => new Allowed();

    public static CheckResult Deny(Denied.Reason cause) => new Denied(cause);
}
