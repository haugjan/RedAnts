namespace RedAnts.Features.Ticketing.Email;

public sealed record FlexMailTicket(System.Guid Uuid, string? Email, string? HolderName, int SeasonId, string CategoryLabel);

public interface IFlexTicketMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }
    Task<EmailSendResult> SendAsync(FlexMailTicket ticket, string subject, string body,
        CancellationToken cancellationToken = default);
}
