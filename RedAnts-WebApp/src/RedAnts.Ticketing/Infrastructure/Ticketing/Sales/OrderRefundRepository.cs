using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class OrderRefundRepository(IScopeProvider scopeProvider) : IOrderRefunds
{
    public async Task<RefundSummary> GetSummaryAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var total = await db.ExecuteScalarAsync<decimal?>("SELECT TotalGross FROM Orders WHERE Id = @0", orderId) ?? 0m;
        var confirmed = await SumAsync(db, orderId, RefundStatus.Confirmed);
        var reserved = await ReservedAsync(db, orderId);
        return new RefundSummary(orderId, total, confirmed, reserved, total - reserved);
    }

    public async Task<IReadOnlyList<OrderRefund>> GetByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<OrderRefundRecord>("WHERE OrderId = @0 ORDER BY Id", orderId);
        return rows.Select(Map).ToList();
    }

    public async Task<OrderRefund> CreateAsync(int orderId, decimal amount, RefundMethod method, RefundStatus initialStatus,
        string? reference, string? reason, string? createdBy)
    {
        if (amount <= 0) throw new DomainException("Rückzahlungsbetrag muss grösser als 0 sein.");
        var value = decimal.Round(amount, 2);

        using var scope = scopeProvider.CreateScope();
        var db = scope.Database;

        var order = await db.SingleOrDefaultAsync<OrderLockRow>(
                        "SELECT Id, TotalGross, Status, VatRate FROM Orders WITH (UPDLOCK, HOLDLOCK) WHERE Id = @0", orderId)
                    ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status is (int)OrderStatus.Draft or (int)OrderStatus.Cancelled)
            throw new DomainException("Nur bezahlte Bestellungen können zurückerstattet werden.");

        var reserved = await ReservedAsync(db, orderId);
        var remaining = order.TotalGross - reserved;
        if (value > remaining)
            throw new DomainException($"Betrag übersteigt den erstattbaren Restbetrag (CHF {remaining:N2}).");

        var refundNumber = await NextRefundNumberAsync(db);
        var refund = OrderRefund.Create(refundNumber, orderId, value, order.VatRate, method, initialStatus, reference, reason, createdBy);
        var record = ToRecord(refund);
        await db.InsertAsync(record);
        var saved = Map(record);

        if (initialStatus == RefundStatus.Confirmed)
            await SettleAsync(db, orderId, order.TotalGross, saved, createdBy);

        scope.Complete();
        return saved;
    }

    public async Task ConfirmAsync(int refundId, string? payrexxRefundId, string? changedBy)
    {
        using var scope = scopeProvider.CreateScope();
        var db = scope.Database;

        var record = await db.SingleOrDefaultByIdAsync<OrderRefundRecord>(refundId)
                     ?? throw new DomainException("Rückzahlung wurde nicht gefunden.");
        if (record.Status == (int)RefundStatus.Confirmed) { scope.Complete(); return; }

        var order = await db.SingleOrDefaultAsync<OrderLockRow>(
                        "SELECT Id, TotalGross, Status, VatRate FROM Orders WITH (UPDLOCK, HOLDLOCK) WHERE Id = @0", record.OrderId)
                    ?? throw new DomainException("Bestellung wurde nicht gefunden.");

        record.Status = (int)RefundStatus.Confirmed;
        if (!string.IsNullOrWhiteSpace(payrexxRefundId)) record.PayrexxRefundId = payrexxRefundId;
        await db.UpdateAsync(record);

        await SettleAsync(db, record.OrderId, order.TotalGross, Map(record), changedBy);
        scope.Complete();
    }

    public async Task FailAsync(int refundId, string? error)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var record = await db.SingleOrDefaultByIdAsync<OrderRefundRecord>(refundId);
        if (record is null || record.Status == (int)RefundStatus.Confirmed) return;
        record.Status = (int)RefundStatus.Failed;
        if (!string.IsNullOrWhiteSpace(error))
            record.Reason = Truncate((string.IsNullOrWhiteSpace(record.Reason) ? "" : record.Reason + " · ") + error.Trim(), 500);
        await db.UpdateAsync(record);
    }

    private static async Task SettleAsync(IDatabase db, int orderId, decimal totalGross, OrderRefund refund, string? changedBy)
    {
        var confirmed = await SumAsync(db, orderId, RefundStatus.Confirmed);
        var status = confirmed >= totalGross ? OrderStatus.Refunded
            : confirmed > 0 ? OrderStatus.PartiallyRefunded : OrderStatus.Paid;

        await db.ExecuteAsync("UPDATE Orders SET Status = @0 WHERE Id = @1", (int)status, orderId);

        var entryNumber = await db.ExecuteScalarAsync<long>("SELECT NEXT VALUE FOR JournalSeq");
        await db.InsertAsync(new AccountingJournalRecord
        {
            EntryNumber = entryNumber,
            EntryType = (int)JournalEntryType.Refund,
            OrderId = orderId,
            RefundId = refund.Id,
            Amount = -refund.Amount,
            VatRate = refund.VatRate,
            VatAmount = -refund.VatAmount,
            Currency = refund.Currency,
            Reference = refund.RefundNumber,
            Description = $"Rückzahlung {refund.Method.DisplayName()}",
            CreatedBy = changedBy,
            OccurredAt = refund.CreatedAt,
            CreatedAt = DateTime.UtcNow
        });

        await db.InsertAsync(new OrderStatusLogRecord
        {
            OrderId = orderId,
            ToStatus = (int)status,
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? null : changedBy.Trim(),
            OccurredAt = DateTime.UtcNow,
            Note = Truncate($"Rückzahlung CHF {refund.Amount:N2} ({refund.Method.DisplayName()}) · {refund.RefundNumber}", 200)
        });
    }

    private static Task<decimal> SumAsync(IDatabase db, int orderId, RefundStatus status) =>
        db.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM OrderRefunds WHERE OrderId = @0 AND Status = @1", orderId, (int)status);

    private static Task<decimal> ReservedAsync(IDatabase db, int orderId) =>
        db.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM OrderRefunds WHERE OrderId = @0 AND Status IN (@1, @2)",
            orderId, (int)RefundStatus.Pending, (int)RefundStatus.Confirmed);

    private static async Task<string> NextRefundNumberAsync(IDatabase db)
    {
        var seq = await db.ExecuteScalarAsync<long>("SELECT NEXT VALUE FOR RefundNumberSeq");
        return $"{SwissTime.Now.Year}-R{seq.ToString("000000", CultureInfo.InvariantCulture)}";
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static OrderRefundRecord ToRecord(OrderRefund r) => new()
    {
        Id = r.Id,
        RefundNumber = r.RefundNumber,
        OrderId = r.OrderId,
        Amount = r.Amount,
        VatRate = r.VatRate,
        VatAmount = r.VatAmount,
        Currency = r.Currency,
        Method = (int)r.Method,
        Status = (int)r.Status,
        PayrexxRefundId = r.PayrexxRefundId,
        Reference = r.Reference,
        Reason = r.Reason,
        CreatedBy = r.CreatedBy,
        CreatedAt = r.CreatedAt
    };

    private static OrderRefund Map(OrderRefundRecord r) => OrderRefund.FromPersistence(
        r.Id, r.RefundNumber, r.OrderId, r.Amount, r.VatRate, r.VatAmount, r.Currency,
        (RefundMethod)r.Method, (RefundStatus)r.Status, r.PayrexxRefundId, r.Reference, r.Reason, r.CreatedBy, r.CreatedAt);

    private sealed class OrderLockRow
    {
        public int Id { get; set; }
        public decimal TotalGross { get; set; }
        public int Status { get; set; }
        public decimal VatRate { get; set; }
    }
}

public sealed class OrderRefundRepositoryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderRefunds, OrderRefundRepository>();
}
