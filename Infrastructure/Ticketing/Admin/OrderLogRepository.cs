using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class OrderLogRepository(IScopeProvider scopeProvider) : IOrderLog
{
    public async Task AppendAsync(int orderId, OrderStatus toStatus, string? changedBy, string? note = null)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.InsertAsync(new OrderStatusLogRecord
        {
            OrderId = orderId,
            ToStatus = (int)toStatus,
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? null : changedBy.Trim(),
            OccurredAt = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
    }

    public async Task<IReadOnlyList<OrderLogEntry>> GetByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<OrderStatusLogRecord>(
            "WHERE OrderId = @0 ORDER BY Id", orderId);
        return rows.Select(r => new OrderLogEntry((OrderStatus)r.ToStatus, r.ChangedBy, r.OccurredAt, r.Note)).ToList();
    }
}

public sealed class OrderLogComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderLog, OrderLogRepository>();
}
