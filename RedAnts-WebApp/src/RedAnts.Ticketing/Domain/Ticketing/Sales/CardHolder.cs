namespace RedAnts.Domain.Ticketing.Sales;

public sealed record CardHolder(
    BuyerType Type,
    string? Salutation,
    string? Company,
    string? FirstName,
    string? LastName,
    DateOnly? Birthday,
    string? Email,
    string? Street,
    string? AddressLine2,
    string? PostalCode,
    string? City,
    string? Country,
    string? Phone)
{
    public static readonly CardHolder Empty =
        new(BuyerType.Private, null, null, null, null, null, null, null, null, null, null, null, null);

    public static CardHolder Create(BuyerType type, string? salutation, string? company, string? firstName,
        string? lastName, DateOnly? birthday, string? email, string? street, string? addressLine2,
        string? postalCode, string? city, string? country, string? phone) =>
        new(type, Clean(salutation), Clean(company), Clean(firstName), Clean(lastName), birthday, Clean(email),
            Clean(street), Clean(addressLine2), Clean(postalCode), Clean(city), Clean(country), Clean(phone));

    public bool IsCompany => !string.IsNullOrWhiteSpace(Company);

    public bool HasName => !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
        || !string.IsNullOrWhiteSpace(Company);

    public bool HasAddress => Street is not null || PostalCode is not null || City is not null
        || Country is not null || AddressLine2 is not null;

    public bool IsEmpty => !HasName && Salutation is null && Birthday is null && Email is null && !HasAddress
        && Phone is null;

    public string? DisplayName => IsCompany
        ? Company
        : $"{FirstName} {LastName}".Trim() is { Length: > 0 } n ? n : null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
