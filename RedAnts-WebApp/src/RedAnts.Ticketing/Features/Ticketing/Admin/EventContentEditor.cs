namespace RedAnts.Features.Ticketing.Admin;

public interface IEventContentEditor
{
    Task SetNameAsync(int eventId, string name);

    Task SetStartAsync(int eventId, DateOnly date, TimeOnly time);
}
