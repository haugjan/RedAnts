namespace RedAnts.Ticketing.Features.Catalog;

public interface IContentUrls
{
    string? GetUrl(int nodeId, bool absolute = false);
}
