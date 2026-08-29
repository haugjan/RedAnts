using Microsoft.Extensions.Logging;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class AccountingJournalBackfillComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, AccountingJournalBackfill>();
}

public sealed class AccountingJournalBackfill(IScopeProvider scopeProvider, ILogger<AccountingJournalBackfill> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var db = scope.Database;

            var sales = await BackfillSalesAsync(db);
            var refunds = await BackfillLegacyRefundsAsync(db);
            if (sales > 0 || refunds > 0)
                logger.LogInformation("Buchungsjournal-Backfill: {Sales} Verkäufe, {Refunds} Alt-Rückerstattungen nachgezogen", sales, refunds);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Buchungsjournal-Backfill übersprungen");
        }
    }

    private static async Task<int> BackfillSalesAsync(IDatabase db)
    {
        var orders = await db.FetchAsync<OrderBackfillRow>(@"
SELECT o.Id, o.OrderNumber, o.TotalGross, o.VatRate, o.VatAmount, o.Currency, o.CreatedAt, o.PaidAt
FROM Orders o
LEFT JOIN AccountingJournal j ON j.OrderId = o.Id AND j.EntryType = @0
WHERE j.Id IS NULL AND (o.Status IN (@1, @2, @3) OR o.PaidAt IS NOT NULL)",
            (int)JournalEntryType.Sale, (int)OrderStatus.Paid, (int)OrderStatus.Refunded, (int)OrderStatus.PartiallyRefunded);

        foreach (var o in orders)
        {
            var entryNumber = await db.ExecuteScalarAsync<long>("SELECT NEXT VALUE FOR JournalSeq");
            await db.InsertAsync(new AccountingJournalRecord
            {
                EntryNumber = entryNumber,
                EntryType = (int)JournalEntryType.Sale,
                OrderId = o.Id,
                RefundId = null,
                Amount = o.TotalGross,
                VatRate = o.VatRate,
                VatAmount = o.VatAmount,
                Currency = string.IsNullOrWhiteSpace(o.Currency) ? "CHF" : o.Currency,
                Reference = o.OrderNumber,
                Description = "Verkauf",
                CreatedBy = null,
                OccurredAt = o.PaidAt ?? o.CreatedAt,
                CreatedAt = DateTime.UtcNow
            });
        }
        return orders.Count;
    }

    private static async Task<int> BackfillLegacyRefundsAsync(IDatabase db)
    {
        var orders = await db.FetchAsync<OrderBackfillRow>(@"
SELECT o.Id, o.OrderNumber, o.TotalGross, o.VatRate, o.VatAmount, o.Currency, o.CreatedAt, o.PaidAt
FROM Orders o
LEFT JOIN OrderRefunds r ON r.OrderId = o.Id
WHERE o.Status = @0 AND r.Id IS NULL AND o.TotalGross > 0",
            (int)OrderStatus.Refunded);

        foreach (var o in orders)
        {
            var when = await db.ExecuteScalarAsync<DateTime?>(
                "SELECT MAX(OccurredAt) FROM OrderStatusLogs WHERE OrderId = @0 AND ToStatus = @1", o.Id, (int)OrderStatus.Refunded)
                ?? o.PaidAt ?? o.CreatedAt;
            var currency = string.IsNullOrWhiteSpace(o.Currency) ? "CHF" : o.Currency;

            var seq = await db.ExecuteScalarAsync<long>("SELECT NEXT VALUE FOR RefundNumberSeq");
            var refundNumber = $"{when.Year}-R{seq:000000}";
            var refund = new OrderRefundRecord
            {
                RefundNumber = refundNumber,
                OrderId = o.Id,
                Amount = o.TotalGross,
                VatRate = o.VatRate,
                VatAmount = o.VatAmount,
                Currency = currency,
                Method = (int)RefundMethod.Manual,
                Status = (int)RefundStatus.Confirmed,
                Reference = null,
                Reason = "Rückwirkend erfasst (Alt-Rückerstattung)",
                CreatedBy = null,
                CreatedAt = when
            };
            await db.InsertAsync(refund);

            var entryNumber = await db.ExecuteScalarAsync<long>("SELECT NEXT VALUE FOR JournalSeq");
            await db.InsertAsync(new AccountingJournalRecord
            {
                EntryNumber = entryNumber,
                EntryType = (int)JournalEntryType.Refund,
                OrderId = o.Id,
                RefundId = refund.Id,
                Amount = -o.TotalGross,
                VatRate = o.VatRate,
                VatAmount = -o.VatAmount,
                Currency = currency,
                Reference = refundNumber,
                Description = "Rückzahlung (rückwirkend)",
                CreatedBy = null,
                OccurredAt = when,
                CreatedAt = DateTime.UtcNow
            });
        }
        return orders.Count;
    }

    private sealed class OrderBackfillRow
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public decimal TotalGross { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public string Currency { get; set; } = "CHF";
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
