using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public sealed record SeasonForAdmin(
    int Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    SeasonStatus Status,
    int EventCount,
    int? PassQuota,
    int? DefaultTicketSalesQuota,
    int PassesSold,
    int TicketsSold,
    int FlexTickets,
    int Admissions,
    int AddOnCount,
    string? PublicUrl,
    string? InternUrl);

public sealed record SeasonsForAdmin(IReadOnlyList<SeasonForAdmin> Seasons, string? CreateSeasonUrl);

public static class GetSeasonsForAdmin
{
    public sealed record Query;

    public sealed class Handler(ISeasonsForAdminReader reader)
    {
        public Task<SeasonsForAdmin> HandleAsync(Query query) => reader.GetAllAsync();
    }
}
