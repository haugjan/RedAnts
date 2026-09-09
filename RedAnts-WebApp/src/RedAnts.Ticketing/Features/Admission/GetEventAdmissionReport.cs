namespace RedAnts.Ticketing.Features.Admission;

public static class GetEventAdmissionReport
{
    public sealed record Query;

    public sealed class Handler(IEventAdmissionReader admissions)
    {
        public Task<IReadOnlyDictionary<int, EventAdmissionCounts>> HandleAsync(Query query) => admissions.GetCountsByEventAsync();
    }
}
