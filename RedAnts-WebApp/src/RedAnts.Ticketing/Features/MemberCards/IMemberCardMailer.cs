using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class MemberCardMailKinds
{
    public static TicketingMailKind For(MemberCategory category) => category switch
    {
        MemberCategory.Block4 => TicketingMailKind.MemberCardBlock4Private,
        MemberCategory.Company => TicketingMailKind.MemberCardBlock4Company,
        _ => TicketingMailKind.MemberCardRedAnts
    };
}

public interface IMemberCardMailer
{
    string DefaultSubjectFor(MemberCategory category);
    string DefaultBodyFor(MemberCategory category);

    Task<EmailSendResult> SendAsync(MemberCard card, string subject, string body, CancellationToken cancellationToken = default);
}
