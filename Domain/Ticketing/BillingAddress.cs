namespace RedAnts.Domain.Ticketing;

public sealed record BillingAddress
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Street { get; }
    public string? AddressLine2 { get; }
    public PostalCode PostalCode { get; }
    public string City { get; }
    public string Country { get; }
    public string Email { get; }
    public string? Phone { get; }

    private BillingAddress(string firstName, string lastName, string street, string? addressLine2,
        PostalCode postalCode, string city, string country, string email, string? phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        AddressLine2 = addressLine2;
        PostalCode = postalCode;
        City = city;
        Country = country;
        Email = email;
        Phone = phone;
    }

    public static BillingAddress Create(string firstName, string lastName, string street,
        string? addressLine2, string postalCode, string city, string? country, string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("Vorname ist erforderlich.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Nachname ist erforderlich.");
        if (string.IsNullOrWhiteSpace(street)) throw new DomainException("Strasse ist erforderlich.");
        if (string.IsNullOrWhiteSpace(city)) throw new DomainException("Ort ist erforderlich.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("Gültige E-Mail-Adresse ist erforderlich.");

        var land = string.IsNullOrWhiteSpace(country) ? "Schweiz" : country.Trim();

        return new BillingAddress(
            firstName.Trim(), lastName.Trim(), street.Trim(),
            string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            PostalCode.Create(postalCode, land), city.Trim(), land,
            email.Trim(), string.IsNullOrWhiteSpace(phone) ? null : phone.Trim());
    }

    public static BillingAddress FromPersistence(string firstName, string lastName, string street,
        string? addressLine2, string postalCode, string city, string country, string email, string? phone) =>
        new(firstName ?? "", lastName ?? "", street ?? "", addressLine2,
            PostalCode.FromPersistence(postalCode), city ?? "", string.IsNullOrWhiteSpace(country) ? "Schweiz" : country,
            email ?? "", phone);

    public string FullName => $"{FirstName} {LastName}".Trim();
}
