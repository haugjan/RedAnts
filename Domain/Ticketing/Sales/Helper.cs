using System.Text.RegularExpressions;

namespace RedAnts.Domain.Ticketing.Sales;

public sealed class Helper
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string Code { get; private set; }
    public bool AllEvents { get; private set; }
    public IReadOnlyList<int> EventIds { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private Helper(int id, int seasonId, string firstName, string lastName, string email, string code,
        bool allEvents, IReadOnlyList<int> eventIds, bool active, DateTime createdAt)
    {
        Id = id;
        SeasonId = seasonId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Code = code;
        AllEvents = allEvents;
        EventIds = eventIds;
        Active = active;
        CreatedAt = createdAt;
    }

    public static Helper Create(int seasonId, string firstName, string lastName, string email, string code)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var fn = (firstName ?? "").Trim();
        var ln = (lastName ?? "").Trim();
        if (fn.Length == 0 && ln.Length == 0) throw new DomainException("Vor- oder Nachname ist erforderlich.");
        var mail = (email ?? "").Trim();
        if (!IsValidEmail(mail)) throw new DomainException("Eine gültige E-Mail-Adresse ist erforderlich.");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Ein Zugangscode ist erforderlich.");
        return new Helper(0, seasonId, fn, ln, mail, code, true, [], true, DateTime.UtcNow);
    }

    public static Helper FromPersistence(int id, int seasonId, string firstName, string lastName, string email,
        string code, bool allEvents, IReadOnlyList<int> eventIds, bool active, DateTime createdAt) =>
        new(id, seasonId, firstName, lastName, email, code, allEvents, eventIds, active, createdAt);

    public static bool IsValidEmail(string? email)
    {
        var value = (email ?? "").Trim();
        if (value.Length == 0 || value.Length > 200) return false;
        return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
