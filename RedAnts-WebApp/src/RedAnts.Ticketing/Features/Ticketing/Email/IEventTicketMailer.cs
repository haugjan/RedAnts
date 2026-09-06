namespace RedAnts.Features.Ticketing.Email;

public sealed record EventMailTicket(System.Guid Uuid, string? Email, string? HolderName, int EventId, string CategoryLabel);

public interface IEventTicketMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }
    Task<EmailSendResult> SendAsync(EventMailTicket ticket, string subject, string body,
        CancellationToken cancellationToken = default);
}
