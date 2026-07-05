namespace RedAnts.Features.Ticketing.Admin;

public sealed record EventLinks(string? Public, string? Intern);

public interface IEventLinkReader
{
    Task<IReadOnlyDictionary<int, EventLinks>> GetBySeasonAsync(int seasonId);
}
