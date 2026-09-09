using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Email;

public interface IHelperInviteMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(
        Helper helper, string subject, string body, string loginLink, CancellationToken cancellationToken = default);
}
