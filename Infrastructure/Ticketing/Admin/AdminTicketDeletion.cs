using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class AdminTicketDeletion(IScopeProvider scopeProvider) : IAdminTicketDeletion
{
    public Task DeleteEventTicketAsync(Guid uuid) => DeleteAsync("EventTickets", uuid);
    public Task DeleteFlexTicketAsync(Guid uuid) => DeleteAsync("SeasonSingleTickets", uuid);
    public Task DeleteSeasonPassAsync(Guid uuid) => DeleteAsync("SeasonPasses", uuid);
    public Task DeleteMemberCardAsync(Guid uuid) => DeleteAsync("MembershipCards", uuid);

    private async Task DeleteAsync(string table, Guid uuid)
    {
        var key = uuid.ToString();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        await db.ExecuteAsync(
            "DELETE FROM TicketEventVisitsLogs WHERE VisitId IN (SELECT Id FROM TicketEventVisits WHERE TicketUuid = @0)", key);
        await db.ExecuteAsync("DELETE FROM TicketEventVisits WHERE TicketUuid = @0", key);
        await db.ExecuteAsync($"DELETE FROM {table} WHERE Uuid = @0", key);
    }
}

public sealed class AdminTicketDeletionComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IAdminTicketDeletion, AdminTicketDeletion>();
}
