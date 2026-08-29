using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Public;

public static class AccessGate
{
    public static bool EventOk(EventStatus status, string? accessSecret, string? providedSecret) =>
        Check(status is EventStatus.Open, status is EventStatus.Intern, accessSecret, providedSecret);

    public static bool SeasonOk(SeasonStatus status, string? accessSecret, string? providedSecret) =>
        Check(status is SeasonStatus.Open, status is SeasonStatus.Intern, accessSecret, providedSecret);

    private static bool Check(bool isOpen, bool isIntern, string? accessSecret, string? providedSecret)
    {
        if (isOpen) return true;
        if (isIntern)
            return !string.IsNullOrWhiteSpace(providedSecret)
                && string.Equals(providedSecret.Trim(), (accessSecret ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        return false;
    }
}
