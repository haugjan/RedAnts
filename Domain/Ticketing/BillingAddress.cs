using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Domain.Ticketing;

public sealed record BillingAddress
{
    public BuyerType Type { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? Company { get; }
    public string Street { get; }
    public string? AddressLine2 { get; }
    public PostalCode PostalCode { get; }
    public string City { get; }
    public string Country { get; }
    public string Email { get; }
    public string? Phone { get; }

    private BillingAddress(BuyerType type, string firstName, string lastName, string? company, string street,
        string? addressLine2, PostalCode postalCode, string city, string country, string email, string? phone)
    {
        Type = type;
        FirstName = firstName;
        LastName = lastName;
        Company = company;
        Street = street;
        AddressLine2 = addressLine2;
        PostalCode = postalCode;
        City = city;
        Country = country;
        Email = email;
        Phone = phone;
    }

    public static BillingAddress Create(BuyerType type, string? firstName, string? lastName, string? company,
        string street, string? addressLine2, string postalCode, string city, string? country, string email, string? phone)
    {
        firstName = (firstName ?? "").Trim();
        lastName = (lastName ?? "").Trim();
        company = string.IsNullOrWhiteSpace(company) ? null : company.Trim();

        if (type == BuyerType.Company)
        {
            if (company is null) throw new DomainException("Bei einer Firma ist der Firmenname erforderlich.");
        }
        else
        {
            if (firstName.Length == 0) throw new DomainException("Vorname ist erforderlich.");
            if (lastName.Length == 0) throw new DomainException("Nachname ist erforderlich.");
        }
        if (string.IsNullOrWhiteSpace(street)) throw new DomainException("Strasse ist erforderlich.");
        if (string.IsNullOrWhiteSpace(city)) throw new DomainException("Ort ist erforderlich.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("Gültige E-Mail-Adresse ist erforderlich.");

        var land = string.IsNullOrWhiteSpace(country) ? "Schweiz" : country.Trim();

        return new BillingAddress(type,
            firstName, lastName, company, street.Trim(),
            string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            PostalCode.Create(postalCode, land), city.Trim(), land,
            email.Trim(), string.IsNullOrWhiteSpace(phone) ? null : phone.Trim());
    }

    public static BillingAddress FromPersistence(int type, string firstName, string lastName, string? company,
        string street, string? addressLine2, string postalCode, string city, string country, string email, string? phone) =>
        new((BuyerType)type, firstName ?? "", lastName ?? "",
            string.IsNullOrWhiteSpace(company) ? null : company, street ?? "", addressLine2,
            PostalCode.FromPersistence(postalCode), city ?? "", string.IsNullOrWhiteSpace(country) ? "Schweiz" : country,
            email ?? "", phone);

    /// <summary>The buyer captured on this address, for copying onto the issued tickets.</summary>
    public Buyer ToBuyer() => Buyer.Create(Type, FirstName, LastName, Company);

    public string FullName => Type == BuyerType.Company
        ? Company ?? ""
        : $"{FirstName} {LastName}".Trim();
}
