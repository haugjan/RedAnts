namespace RedAnts.Domain.Ticketing;

public sealed class Venue
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? GoogleGeoId { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Description { get; private set; }
    public string? Address { get; private set; }

    private Venue(int id, string name, string? googleGeoId, string? imageUrl, string? description, string? address)
    {
        Id = id;
        Name = name;
        GoogleGeoId = googleGeoId;
        ImageUrl = imageUrl;
        Description = description;
        Address = address;
    }

    public static Venue Create(string name, string? googleGeoId, string? imageUrl, string? description, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name des Orts ist erforderlich.");
        return new Venue(0, name.Trim(), Clean(googleGeoId), Clean(imageUrl), Clean(description), Clean(address));
    }

    public static Venue FromPersistence(int id, string name, string? googleGeoId, string? imageUrl, string? description, string? address = null) =>
        new(id, name ?? "", googleGeoId, imageUrl, description, address);

    public void Update(string name, string? googleGeoId, string? imageUrl, string? description, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name des Orts ist erforderlich.");
        Name = name.Trim();
        GoogleGeoId = Clean(googleGeoId);
        ImageUrl = Clean(imageUrl);
        Description = Clean(description);
        Address = Clean(address);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
