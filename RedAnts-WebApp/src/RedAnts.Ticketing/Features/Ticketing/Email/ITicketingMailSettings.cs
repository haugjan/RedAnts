namespace RedAnts.Features.Ticketing.Email;

public enum TicketingMailKind
{
    HelperInvite,
    MemberCardRedAnts,
    MemberCardBlock4Private,
    MemberCardBlock4Company,
    SeasonPass,
    EventTicket,
    FlexTicket
}

public interface ITicketingMailSettings
{
    string Subject(TicketingMailKind kind, string fallback);
    string Body(TicketingMailKind kind, string fallback);
}
