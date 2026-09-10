namespace RedAnts.Ticketing.Domain.Sales;

public sealed class Helper
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public EmailAddress Email { get; private set; }
    public string Code { get; private set; }
    public bool AllEvents { get; private set; }
    public IReadOnlyList<int> EventIds { get; private set; }
    public bool CanRebook { get; private set; }
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private Helper(int id, int seasonId, string firstName, string lastName, EmailAddress email, string code,
        bool allEvents, IReadOnlyList<int> eventIds, bool canRebook, bool active, DateTimeOffset createdAt)
    {
        Id = id;
        SeasonId = seasonId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Code = code;
        AllEvents = allEvents;
        EventIds = eventIds;
        CanRebook = canRebook;
        Active = active;
        CreatedAt = createdAt;
    }

    public static Helper Create(int seasonId, string firstName, string lastName, string email, string code, TimeProvider? time = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var fn = (firstName ?? "").Trim();
        var ln = (lastName ?? "").Trim();
        if (fn.Length == 0 && ln.Length == 0) throw new DomainException("Vor- oder Nachname ist erforderlich.");
        var mail = EmailAddress.Create(email);
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Ein Zugangscode ist erforderlich.");
        return new Helper(0, seasonId, fn, ln, mail, code, true, [], false, true, SwissTime.TimestampOf(time));
    }

    public static Helper FromPersistence(int id, int seasonId, string firstName, string lastName, string email,
        string code, bool allEvents, IReadOnlyList<int> eventIds, bool canRebook, bool active, DateTimeOffset createdAt) =>
        new(id, seasonId, firstName, lastName, EmailAddress.TryCreate(email) ?? default, code, allEvents, eventIds,
            canRebook, active, createdAt);
}
