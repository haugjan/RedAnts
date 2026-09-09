namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventConversionRuleReader
{
    Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId);

    Task<bool> GetConversionOnlyAsync(int eventId);
}
