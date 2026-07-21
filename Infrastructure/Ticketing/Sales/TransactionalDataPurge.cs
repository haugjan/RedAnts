using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedAnts.Infrastructure.Ticketing.Content;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class TransactionalDataPurgeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TransactionalDataPurge>();
}

public sealed class TransactionalDataPurge(
    IScopeProvider scopeProvider,
    IMemberService memberService,
    IKeyValueService keyValueService,
    IConfiguration config,
    ILogger<TransactionalDataPurge> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly string[] Tables =
    [
        "TicketEventVisitsLogs",
        "TicketEventVisits",
        "TicketEventFreeEntries",
        "OrderItems",
        "OrderAddOns",
        "OrderStatusLogs",
        "EventTickets",
        "SeasonSingleTickets",
        "SeasonPasses",
        "MembershipCards",
        "FlexTicketBundles",
        "EventTicketBundles",
        "Orders",
    ];

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var token = (config["Ticketing:PurgeTransactionalDataToken"] ?? "").Trim();
        if (token.Length == 0) return;

        var stateKey = $"ticketing-purge:{token}";
        if (!string.IsNullOrEmpty(keyValueService.GetValue(stateKey))) return;

        var deleted = new Dictionary<string, int>();
        int fixedFreeCleared;
        using (var scope = scopeProvider.CreateScope(autoComplete: true))
        {
            var db = scope.Database;
            foreach (var table in Tables)
                deleted[table] = await db.ExecuteAsync($"DELETE FROM {table}");

            fixedFreeCleared = await db.ExecuteAsync(
                "UPDATE TicketEventFreeEntryQuotas SET PlayerFixed = NULL, StaffFixed = NULL, " +
                "OfficialFixed = NULL, SuFixed = NULL, ChildFixed = NULL " +
                "WHERE PlayerFixed IS NOT NULL OR StaffFixed IS NOT NULL OR OfficialFixed IS NOT NULL " +
                "OR SuFixed IS NOT NULL OR ChildFixed IS NOT NULL");
        }

        var helpers = memberService.GetMembersByMemberType(HelperAliases.MemberType).ToList();
        foreach (var helper in helpers)
            memberService.Delete(helper);

        keyValueService.SetValue(stateKey, DateTime.UtcNow.ToString("o"));

        logger.LogWarning("Ticketing-Transaktionsdaten geleert (Token {Token}): {Helpers} Helfer gelöscht, {FixedFree} Fix-Freieintritt-Zeilen zurückgesetzt, Tabellen {Detail}",
            token, helpers.Count, fixedFreeCleared, string.Join(", ", deleted.Select(kv => $"{kv.Key}={kv.Value}")));
    }
}
