using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Email;

public interface ISeasonPassMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(SeasonPass pass, string categoryLabel, string subject, string body,
        CancellationToken cancellationToken = default);
}
