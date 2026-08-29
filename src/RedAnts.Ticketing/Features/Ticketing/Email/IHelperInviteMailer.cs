using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Email;

public interface IHelperInviteMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(
        Helper helper, string subject, string body, string loginLink, CancellationToken cancellationToken = default);
}
