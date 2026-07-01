using RedAnts.Domain.Ticketing;

namespace RedAnts.Infrastructure.Ticketing;

/// <summary>Shared conversions between persistence records and domain types.</summary>
internal static class TicketingMappers
{
    public static DateTime ToDateTime(this DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
    public static DateOnly ToDateOnly(this DateTime value) => DateOnly.FromDateTime(value);

    public static string ToHm(this TimeOnly time) => time.ToString("HH\\:mm", System.Globalization.CultureInfo.InvariantCulture);
    public static TimeOnly ParseHm(string value) =>
        TimeOnly.TryParseExact(value, "HH\\:mm", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var t) ? t : TimeOnly.MinValue;

    public static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;

    public static BillingAddress ReadBilling(string firstName, string lastName, string street, string? line2,
        string postalCode, string city, string country, string email, string? phone) =>
        BillingAddress.FromPersistence(firstName, lastName, street, line2, postalCode, city, country, email, phone);
}
