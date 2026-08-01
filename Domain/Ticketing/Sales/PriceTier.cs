namespace RedAnts.Domain.Ticketing.Sales;

public sealed class PriceTier
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public string Name { get; private set; }
    public int? MinAge { get; private set; }
    public int? MaxAge { get; private set; }
    public int? PromoOfTierId { get; private set; }
    public int SortOrder { get; private set; }

    private PriceTier(int id, int seasonId, string name, int? minAge, int? maxAge, int? promoOfTierId, int sortOrder)
    {
        Id = id;
        SeasonId = seasonId;
        Name = name;
        MinAge = minAge;
        MaxAge = maxAge;
        PromoOfTierId = promoOfTierId;
        SortOrder = sortOrder;
    }

    public bool IsPromo => PromoOfTierId is not null;

    public static PriceTier Create(int seasonId, string name, int? minAge, int? maxAge, int? promoOfTierId, int sortOrder)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        name = (name ?? "").Trim();
        if (name.Length == 0) throw new DomainException("Ein Stufenname muss angegeben werden.");
        if (name.Length > 200) throw new DomainException("Der Stufenname darf höchstens 200 Zeichen lang sein.");
        if (minAge is < 0) throw new DomainException("Das Alter darf nicht negativ sein.");
        if (maxAge is < 0) throw new DomainException("Das Alter darf nicht negativ sein.");
        if (minAge is { } lo && maxAge is { } hi && lo > hi)
            throw new DomainException("Das Alter «von» darf nicht grösser als das Alter «bis» sein.");
        return new PriceTier(0, seasonId, name, minAge, maxAge, promoOfTierId, sortOrder);
    }

    public static PriceTier FromPersistence(int id, int seasonId, string name, int? minAge, int? maxAge, int? promoOfTierId, int sortOrder) =>
        new(id, seasonId, name, minAge, maxAge, promoOfTierId, sortOrder);
}
