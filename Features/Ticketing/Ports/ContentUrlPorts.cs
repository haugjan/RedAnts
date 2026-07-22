namespace RedAnts.Features.Ticketing.Ports;

public interface IContentUrls
{
    string? GetUrl(int nodeId, bool absolute = false);
}
