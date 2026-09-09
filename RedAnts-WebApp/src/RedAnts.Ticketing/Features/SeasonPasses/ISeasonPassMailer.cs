using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public interface ISeasonPassMailer
{
    string DefaultSubject { get; }
    string DefaultBody { get; }

    Task<EmailSendResult> SendAsync(SeasonPass pass, string categoryLabel, string subject, string body,
        CancellationToken cancellationToken = default);
}
