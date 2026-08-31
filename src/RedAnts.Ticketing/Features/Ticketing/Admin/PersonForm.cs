using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed class PersonForm
{
    public BuyerType BuyerType { get; set; } = BuyerType.Private;
    public string? Company { get; set; }
    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Street { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public DateOnly? Birthday { get; set; }

    public bool IsCompany => BuyerType == BuyerType.Company;

    public static PersonForm From(CardHolder? h) => h is null
        ? new PersonForm()
        : new PersonForm
        {
            BuyerType = h.IsCompany ? BuyerType.Company : BuyerType.Private,
            Company = h.Company,
            Salutation = h.Salutation,
            FirstName = h.FirstName,
            LastName = h.LastName,
            Email = h.Email,
            Street = h.Street,
            AddressLine2 = h.AddressLine2,
            PostalCode = h.PostalCode,
            City = h.City,
            Country = h.Country,
            Phone = h.Phone,
            Birthday = h.Birthday
        };

    public CardHolder ToHolder() => CardHolder.Create(
        BuyerType, Salutation, Company, FirstName, LastName, Birthday, Email,
        Street, AddressLine2, PostalCode, City, Country, Phone);

    public MemberAddress ToMemberAddress() =>
        MemberAddress.Create(Salutation, Company, Street, AddressLine2, PostalCode, City, Country, Phone);

    public Buyer? ToBuyer()
    {
        if (IsCompany)
            return string.IsNullOrWhiteSpace(Company) ? null : Buyer.Create(BuyerType.Company, null, null, Company.Trim());
        return string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? null
            : Buyer.Create(BuyerType.Private, FirstName, LastName, null);
    }
}
