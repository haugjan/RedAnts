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
        MemberCategory category, TicketStatus status, string? reference, string? email = null)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE MembershipCards SET FirstName = @0, LastName = @1, Birthday = @2, " +
            "Category = @3, Status = @4, Reference = @5, Email = @6 WHERE Uuid = @7",
            (object[])new object?[]
            {
                string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim(),
                string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
                birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                (int)category,
                (int)status,
                string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
                string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                uuid.ToString()
            });
    }
}

public sealed class MemberCardEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IMemberCardEditor, MemberCardEditorAdapter>();
}
