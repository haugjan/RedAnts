namespace RedAnts.Features.Ticketing.Admin;

public interface IContentCreateLinks
{
    Task<string?> CreateSeasonUrlAsync();
    Task<string?> CreateEventUrlAsync(int seasonId);
}
