using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Email;

public interface IMemberCardMailer
{
    string DefaultSubjectFor(MemberCategory category, bool isCompany);
    string DefaultBodyFor(MemberCategory category, bool isCompany);

    Task<EmailSendResult> SendAsync(MemberCard card, string subject, string body, CancellationToken cancellationToken = default);
}
