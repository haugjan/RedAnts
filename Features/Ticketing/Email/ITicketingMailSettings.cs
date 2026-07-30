namespace RedAnts.Features.Ticketing.Email;

public enum TicketingMailKind
{
    HelperInvite,
    MemberCard,
    SeasonPass
}

public interface ITicketingMailSettings
{
    string Subject(TicketingMailKind kind, string fallback);
    string Body(TicketingMailKind kind, string fallback);
}
