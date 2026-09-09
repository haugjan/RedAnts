using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.Helpers;

public interface IHelperInviteMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(
        Helper helper, string subject, string body, string loginLink, CancellationToken cancellationToken = default);
}
