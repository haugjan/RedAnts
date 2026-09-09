using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class GetMemberCardMailTemplate
{
    public sealed record Query(MemberCategory Category);

    public sealed class Handler(IMemberCardMailer mailer)
    {
        public Task<MailTemplate> HandleAsync(Query query) =>
            Task.FromResult(new MailTemplate(mailer.DefaultSubjectFor(query.Category), mailer.DefaultBodyFor(query.Category)));
    }
}
