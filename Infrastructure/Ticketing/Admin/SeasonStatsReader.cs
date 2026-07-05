using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class SeasonStatsReader(IScopeProvider scopeProvider) : ISeasonStatsReader
{
    public async Task<SeasonStats> GetAsync(int seasonId, IReadOnlyList<int> eventIds)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var passesSold = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonPasses WHERE SeasonId = @0 AND Status = @1",
            seasonId, (int)TicketStatus.Valid);

        var flexSold = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets WHERE SeasonId = @0 AND Status = @1",
            seasonId, (int)TicketStatus.Valid);

        var eventTicketsSold = 0;
        var admissions = 0;
        if (eventIds.Count > 0)
        {
            eventTicketsSold = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM EventTickets WHERE EventId IN (@0) AND Status = @1",
                eventIds, (int)TicketStatus.Valid);
            admissions = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId IN (@0)", eventIds);
        }

        return new SeasonStats(passesSold, eventTicketsSold + flexSold, admissions);
    }
}

public sealed class SeasonStatsReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonStatsReader, SeasonStatsReader>();
}
