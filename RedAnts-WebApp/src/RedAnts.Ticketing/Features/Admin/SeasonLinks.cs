namespace RedAnts.Ticketing.Features.Admin;

public sealed record SeasonLinks(string? Public, string? Intern);

public interface ISeasonLinkReader
{
    Task<IReadOnlyDictionary<int, SeasonLinks>> GetAllAsync();
}
