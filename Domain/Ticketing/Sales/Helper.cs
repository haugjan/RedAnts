namespace RedAnts.Domain.Ticketing.Sales;

public sealed class Helper
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Email { get; private set; }
    public string Password { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private Helper(int id, int seasonId, string firstName, string lastName, string? email,
        string password, bool active, DateTime createdAt)
    {
        Id = id;
        SeasonId = seasonId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
        Active = active;
        CreatedAt = createdAt;
    }

    public static Helper Create(int seasonId, string firstName, string lastName, string? email, string password)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var fn = (firstName ?? "").Trim();
        var ln = (lastName ?? "").Trim();
        if (fn.Length == 0 && ln.Length == 0) throw new DomainException("Vor- oder Nachname ist erforderlich.");
        if (string.IsNullOrWhiteSpace(password)) throw new DomainException("Ein Passwort ist erforderlich.");
        return new Helper(0, seasonId, fn, ln,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(), password, true, DateTime.UtcNow);
    }

    public static Helper FromPersistence(int id, int seasonId, string firstName, string lastName, string? email,
        string password, bool active, DateTime createdAt) =>
        new(id, seasonId, firstName, lastName, email, password, active, createdAt);
}
