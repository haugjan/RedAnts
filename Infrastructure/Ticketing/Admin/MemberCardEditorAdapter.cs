using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Updates the editable columns of one member card in <c>MembershipCards</c>, matched by Uuid.
/// Uses an explicit column UPDATE (not the NPoco record) so it is unaffected by the record's in-flight
/// column changes (e.g. the dropped Price column). Enums are stored as their int value.</summary>
public sealed class MemberCardEditorAdapter(IScopeProvider scopeProvider) : IMemberCardEditor
{
    public async Task SetDetailsAsync(Guid uuid, string? firstName, string? lastName, DateOnly? birthday,
        MemberCategory category, TicketStatus status, string? reference)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE MembershipCards SET FirstName = @0, LastName = @1, Birthday = @2, " +
            "Category = @3, Status = @4, Reference = @5 WHERE Uuid = @6",
            new object?[]
            {
                string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim(),
                string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
                birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                (int)category,
                (int)status,
                string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
                uuid.ToString()
            });
    }
}

/// <summary>Registers the Mitglieder edit adapter (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class MemberCardEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IMemberCardEditor, MemberCardEditorAdapter>();
}
