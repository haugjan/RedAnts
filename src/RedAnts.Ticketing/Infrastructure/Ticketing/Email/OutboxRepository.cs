using System.Text.Json;
using NPoco;
using RedAnts.Features.Ticketing.Email;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class OutboxRepository(IScopeProvider scopeProvider, OutboxSignal signal)
    : IEmailOutbox, IOutboxAdminReport
{
    public async Task EnqueueAsync(OutboxEnqueueRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        using (var scope = scopeProvider.CreateScope(autoComplete: true))
        {
            await scope.Database.InsertAsync(new OutboxEmailRecord
            {
                Uuid = Guid.NewGuid().ToString(),
                ToEmail = request.ToEmail,
                ToName = request.ToName,
                Subject = request.Subject,
                BodyHtml = request.HtmlBody,
                AttachmentsJson = Serialize(request.Attachments),
                Status = (int)OutboxStatus.Pending,
                Attempts = 0,
                SentVia = null,
                LastError = null,
                Source = request.Source,
                Reference = request.Reference,
                CreatedAt = now,
                NextAttemptAt = now,
                SentAt = null
            });
        }
        signal.Notify();
    }

    public async Task<OutboxMessage?> ClaimNextDueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var claimed = await scope.Database.FetchAsync<OutboxEmailRecord>(
            ";WITH due AS (SELECT TOP(1) Id FROM OutboxEmails WITH (READPAST, UPDLOCK, ROWLOCK) " +
            "WHERE Status = 0 AND NextAttemptAt <= @0 ORDER BY NextAttemptAt, Id) " +
            "UPDATE o SET Status = 1, Attempts = o.Attempts + 1 " +
            "OUTPUT inserted.Id, inserted.ToEmail, inserted.ToName, inserted.Subject, inserted.BodyHtml, " +
            "inserted.AttachmentsJson, inserted.Attempts, inserted.SentVia " +
            "FROM OutboxEmails o INNER JOIN due ON o.Id = due.Id", now);

        var row = claimed.FirstOrDefault();
        if (row is null) return null;

        return new OutboxMessage(
            row.Id, row.ToEmail, row.ToName, row.Subject, row.BodyHtml,
            Deserialize(row.AttachmentsJson), row.Attempts, row.SentVia);
    }

    public async Task MarkSentAsync(int id, string sentVia, DateTime sentAt)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE OutboxEmails SET Status = 2, SentVia = @1, SentAt = @2, LastError = NULL WHERE Id = @0",
            id, sentVia, sentAt);
    }

    public async Task RescheduleAsync(int id, string? sentVia, string lastError, DateTime nextAttemptAt)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE OutboxEmails SET Status = 0, SentVia = @1, LastError = @2, NextAttemptAt = @3 WHERE Id = @0",
            id, sentVia, Trim(lastError), nextAttemptAt);
    }

    public async Task MarkFailedAsync(int id, string? sentVia, string lastError)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE OutboxEmails SET Status = 3, SentVia = @1, LastError = @2 WHERE Id = @0",
            id, sentVia, Trim(lastError));
    }

    public async Task<int> PurgeSentBeforeAsync(DateTime cutoff)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.ExecuteAsync(
            "DELETE FROM OutboxEmails WHERE Status = 2 AND SentAt IS NOT NULL AND SentAt < @0", cutoff);
    }

    public async Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTime sentSince)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var where = includeSent
            ? "WHERE Status <> 2 OR (Status = 2 AND SentAt >= @0)"
            : "WHERE Status <> 2";
        var rows = await scope.Database.FetchAsync<OutboxEmailRecord>(
            "SELECT Id, ToEmail, ToName, Subject, Status, Attempts, SentVia, LastError, Source, Reference, " +
            "CreatedAt, NextAttemptAt, SentAt FROM OutboxEmails " + where + " ORDER BY CreatedAt DESC", sentSince);

        return rows.Select(r => new OutboxEntry(
            r.Id, r.ToEmail, r.ToName, r.Subject, (OutboxStatus)r.Status, r.Attempts, r.SentVia, r.LastError,
            r.Source, r.Reference, r.CreatedAt, r.NextAttemptAt, r.SentAt)).ToList();
    }

    public async Task<bool> RequeueAsync(int id)
    {
        int affected;
        using (var scope = scopeProvider.CreateScope(autoComplete: true))
        {
            affected = await scope.Database.ExecuteAsync(
                "UPDATE OutboxEmails SET Status = 0, NextAttemptAt = @1, LastError = NULL WHERE Id = @0 AND Status <> 2",
                id, DateTime.UtcNow);
        }
        if (affected > 0) signal.Notify();
        return affected > 0;
    }

    private static string? Serialize(IReadOnlyList<EmailAttachment>? attachments) =>
        attachments is { Count: > 0 } ? JsonSerializer.Serialize(attachments) : null;

    private static IReadOnlyList<EmailAttachment>? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<EmailAttachment>>(json);

    private static string Trim(string value) => value.Length > 1000 ? value[..1000] : value;
}
