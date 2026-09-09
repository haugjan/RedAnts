namespace RedAnts.Ticketing.Features.Ports;

public interface IContentUrls
{
    string? GetUrl(int nodeId, bool absolute = false);
}
