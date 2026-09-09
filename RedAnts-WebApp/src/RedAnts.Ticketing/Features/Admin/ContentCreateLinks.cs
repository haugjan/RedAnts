namespace RedAnts.Ticketing.Features.Admin;

public interface IContentCreateLinks
{
    Task<string?> CreateSeasonUrlAsync();
    Task<string?> CreateEventUrlAsync(int seasonId);
}
