using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Email;

public interface ISeasonPassMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(SeasonPass pass, string categoryLabel, string subject, string body,
        CancellationToken cancellationToken = default);
}
