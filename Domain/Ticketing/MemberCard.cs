namespace RedAnts.Domain.Ticketing;

/// <summary>A member card (Mitgliederkarte) identifying a member within a season.</summary>
public sealed class MemberCard
{
    public int Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly Birthday { get; private set; }
    public int SeasonId { get; private set; }

    private MemberCard(int id, string firstName, string lastName, DateOnly birthday, int seasonId)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Birthday = birthday;
        SeasonId = seasonId;
    }

    public static MemberCard Create(string firstName, string lastName, DateOnly birthday, int seasonId)
    {
        Validate(firstName, lastName, seasonId);
        return new MemberCard(0, firstName.Trim(), lastName.Trim(), birthday, seasonId);
    }

    public static MemberCard FromPersistence(int id, string firstName, string lastName, DateOnly birthday, int seasonId) =>
        new(id, firstName ?? "", lastName ?? "", birthday, seasonId);

    public void Update(string firstName, string lastName, DateOnly birthday, int seasonId)
    {
        Validate(firstName, lastName, seasonId);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Birthday = birthday;
        SeasonId = seasonId;
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private static void Validate(string firstName, string lastName, int seasonId)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("Vorname ist erforderlich.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Nachname ist erforderlich.");
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
    }
}
