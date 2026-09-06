using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class MemberCardOrderCleanupComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, MemberCardOrderCleanup>();
}

public sealed class MemberCardOrderCleanup(IScopeProvider scopeProvider, ILogger<MemberCardOrderCleanup> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const int MemberCardKind = 3;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var linked = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MembershipCards WHERE OrderId IS NOT NULL");
        var positions = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM OrderItems WHERE Kind = @0", MemberCardKind);
        if (linked == 0 && positions == 0) return;

        if (linked > 0) await db.ExecuteAsync("UPDATE MembershipCards SET OrderId = NULL WHERE OrderId IS NOT NULL");
        if (positions > 0) await db.ExecuteAsync("DELETE FROM OrderItems WHERE Kind = @0", MemberCardKind);

        logger.LogInformation("Mitgliederkarten von Bestellungen entkoppelt: {Cards} Karten, {Positions} Positionen entfernt",
            linked, positions);
    }
}
