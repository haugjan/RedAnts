using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class MemberCardEditorAdapter(IScopeProvider scopeProvider) : IMemberCardEditor
{
    public async Task SetDetailsAsync(Guid uuid, string? firstName, string? lastName, DateOnly? birthday,
        MemberCategory category, TicketStatus status, string? reference, string? email = null,
        MemberAddress? address = null, int admissions = 1)
    {
        var a = address ?? MemberAddress.Empty;
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE MembershipCards SET FirstName = @0, LastName = @1, Birthday = @2, " +
            "Category = @3, Status = @4, Reference = @5, Email = @6, " +
            "Salutation = @7, Company = @8, Street = @9, AddressLine2 = @10, PostalCode = @11, City = @12, Country = @13, Phone = @14, " +
            "Admissions = @15 WHERE Uuid = @16",
            (object[])new object?[]
            {
                string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim(),
                string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
                birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                (int)category,
                (int)status,
                string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
                string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                a.Salutation, a.Company, a.Street, a.AddressLine2, a.PostalCode, a.City, a.Country, a.Phone,
                Math.Max(1, admissions),
                uuid.ToString()
            });
    }
}

public sealed class MemberCardEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IMemberCardEditor, MemberCardEditorAdapter>();
}
