using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Admission.Admin;

namespace RedAnts.Ticketing.Features.Catalog;

public sealed record EventForAdmin(
    int Id,
    string Name,
    DateOnly Date,
    TimeOnly StartTime,
    bool TimeUnknown,
    EventStatus Status,
    int? AdmissionQuota,
    int? SalesQuota,
    EventAdmissionCounts Counts,
    string? PublicUrl,
    string? InternUrl,
    bool SeasonPassRequired,
    bool MemberCardRequired,
    decimal? FlexDiscount,
    bool ConversionOnly);

public sealed record EventsForAdmin(IReadOnlyList<EventForAdmin> Events, string? CreateEventUrl);

public static class GetEventsForAdmin
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(IEventsForAdminReader reader)
    {
        public Task<EventsForAdmin> HandleAsync(Query query) => reader.GetBySeasonAsync(query.SeasonId);
    }
}
