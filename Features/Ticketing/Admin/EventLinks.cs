namespace RedAnts.Features.Ticketing.Admin;

/// <summary>The public and internal share links of an event, as stored on its content node. Both are
/// relative URLs; the intern link carries the access secret as a query string. Either may be null when
/// the node has not been published yet.</summary>
public sealed record EventLinks(string? Public, string? Intern);

/// <summary>Read side for the event share links shown (as copy buttons) in the Anlässe admin table.</summary>
public interface IEventLinkReader
{
    Task<IReadOnlyDictionary<int, EventLinks>> GetBySeasonAsync(int seasonId);
}
