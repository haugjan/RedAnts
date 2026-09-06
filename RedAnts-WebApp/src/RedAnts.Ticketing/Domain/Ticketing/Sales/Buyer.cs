namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>
/// Who a ticket or season pass is bought by. Either a private person (first + last name required)
/// or a company (company name required). Stored flattened onto the owning entity.
/// </summary>
public sealed record Buyer
{
    public BuyerType Type { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? Company { get; }

    private Buyer(BuyerType type, string? firstName, string? lastName, string? company)
    {
        Type = type;
        FirstName = firstName;
        LastName = lastName;
        Company = company;
    }

    /// <summary>Display label: the company name, or "First Last" for a private person.</summary>
    public string DisplayName => Type == BuyerType.Company
        ? Company ?? ""
        : $"{FirstName} {LastName}".Trim();

    public static Buyer Create(BuyerType type, string? firstName, string? lastName, string? company)
    {
        firstName = Clean(firstName);
        lastName = Clean(lastName);
        company = Clean(company);

        if (type == BuyerType.Company)
        {
            if (company is null) throw new DomainException("Bei einer Firma ist der Firmenname erforderlich.");
            return new Buyer(BuyerType.Company, null, null, company);
        }

        if (firstName is null || lastName is null)
            throw new DomainException("Bei einer Privatperson sind Vor- und Nachname erforderlich.");
        return new Buyer(BuyerType.Private, firstName, lastName, null);
    }

    /// <summary>Rehydrate from storage. Returns null when no buyer was ever recorded (legacy rows).</summary>
    public static Buyer? FromPersistence(int type, string? firstName, string? lastName, string? company)
    {
        firstName = Clean(firstName);
        lastName = Clean(lastName);
        company = Clean(company);
        if (firstName is null && lastName is null && company is null) return null;
        return new Buyer((BuyerType)type, firstName, lastName, company);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
