using RedAnts.Features.Ticketing.Email;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class TicketingMailSettings(IPublishedContentQuery query, IUmbracoContextFactory contextFactory)
    : ITicketingMailSettings
{
    public string Subject(TicketingMailKind kind, string fallback) => Read(SubjectAlias(kind), fallback);

    public string Body(TicketingMailKind kind, string fallback) => Read(BodyAlias(kind), fallback);

    private string Read(string alias, string fallback)
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var node = query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias == A.RootType)
            ?.Children().FirstOrDefault(c => c.ContentType.Alias == A.MailTextsType);
        var value = node?.Value<string>(alias);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string SubjectAlias(TicketingMailKind kind) => kind switch
    {
        TicketingMailKind.MemberCardRedAnts => A.MemberCardMailSubject,
        TicketingMailKind.MemberCardBlock4Private => A.MemberCardBlock4PrivateMailSubject,
        TicketingMailKind.MemberCardBlock4Company => A.MemberCardBlock4CompanyMailSubject,
        TicketingMailKind.SeasonPass => A.SeasonPassMailSubject,
        _ => A.HelperInviteMailSubject
    };

    private static string BodyAlias(TicketingMailKind kind) => kind switch
    {
        TicketingMailKind.MemberCardRedAnts => A.MemberCardMailBody,
        TicketingMailKind.MemberCardBlock4Private => A.MemberCardBlock4PrivateMailBody,
        TicketingMailKind.MemberCardBlock4Company => A.MemberCardBlock4CompanyMailBody,
        TicketingMailKind.SeasonPass => A.SeasonPassMailBody,
        _ => A.HelperInviteMailBody
    };
}
