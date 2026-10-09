namespace RedAnts.Game.Features.Round;

public interface IRoundReader
{
    Task<IReadOnlyList<RoundHighlight>> GetHighlightsAsync();
}
