namespace RedAnts.Ticketing.Features.Admin;

public sealed record EventLinks(string? Public, string? Intern);

public interface IEventLinkReader
{
    Task<IReadOnlyDictionary<int, EventLinks>> GetBySeasonAsync(int seasonId);
}
