namespace RedAnts.Domain.Ticketing;

public sealed class Venue
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? GoogleGeoId { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Description { get; private set; }

    private Venue(int id, string name, string? googleGeoId, string? imageUrl, string? description)
    {
        Id = id;
        Name = name;
        GoogleGeoId = googleGeoId;
        ImageUrl = imageUrl;
        Description = description;
    }

    public static Venue Create(string name, string? googleGeoId, string? imageUrl, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name des Orts ist erforderlich.");
        return new Venue(0, name.Trim(), Clean(googleGeoId), Clean(imageUrl), Clean(description));
    }

    public static Venue FromPersistence(int id, string name, string? googleGeoId, string? imageUrl, string? description) =>
        new(id, name ?? "", googleGeoId, imageUrl, description);

    public void Update(string name, string? googleGeoId, string? imageUrl, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name des Orts ist erforderlich.");
        Name = name.Trim();
        GoogleGeoId = Clean(googleGeoId);
        ImageUrl = Clean(imageUrl);
        Description = Clean(description);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
