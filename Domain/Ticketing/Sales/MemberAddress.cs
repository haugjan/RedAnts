namespace RedAnts.Domain.Ticketing.Sales;

public sealed record MemberAddress(
    string? Salutation,
    string? Company,
    string? Street,
    string? AddressLine2,
    string? PostalCode,
    string? City,
    string? Country,
    string? Phone)
{
    public static readonly MemberAddress Empty = new(null, null, null, null, null, null, null, null);

    public static MemberAddress Create(string? salutation, string? company, string? street, string? addressLine2,
        string? postalCode, string? city, string? country, string? phone) =>
        new(Clean(salutation), Clean(company), Clean(street), Clean(addressLine2),
            Clean(postalCode), Clean(city), Clean(country), Clean(phone));

    public bool IsEmpty =>
        Salutation is null && Company is null && Street is null && AddressLine2 is null
        && PostalCode is null && City is null && Country is null && Phone is null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
