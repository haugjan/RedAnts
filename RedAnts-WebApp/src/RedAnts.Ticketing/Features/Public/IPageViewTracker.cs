namespace RedAnts.Ticketing.Features.Public;

public readonly record struct PageView(DateTimeOffset OccurredAt, string Path, string? VisitorHash, bool IsBot);

public interface IPageViewTracker
{
    void Track(PageView view);
}
