using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Email;

public interface IMemberCardMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(MemberCard card, string subject, string body, CancellationToken cancellationToken = default);
}
