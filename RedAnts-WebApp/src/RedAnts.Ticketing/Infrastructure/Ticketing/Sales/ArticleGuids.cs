namespace RedAnts.Infrastructure.Ticketing.Sales;

internal sealed class ArticleGuids(IReadOnlyDictionary<string, Guid> existing)
{
    public static ArticleGuids ByTierAndCategory<T>(IEnumerable<T> rows, Func<T, int?> tierId, Func<T, int> category, Func<T, Guid?> articleGuid)
    {
        var map = new Dictionary<string, Guid>();
        foreach (var row in rows)
            if (articleGuid(row) is { } guid)
                map[TierKey(tierId(row), category(row))] = guid;
        return new ArticleGuids(map);
    }

    public static ArticleGuids ByIdOrLabel<T>(IEnumerable<T> rows, Func<T, int> id, Func<T, string> label, Func<T, Guid?> articleGuid)
    {
        var map = new Dictionary<string, Guid>();
        foreach (var row in rows)
        {
            if (articleGuid(row) is not { } guid) continue;
            map[IdKey(id(row))] = guid;
            map.TryAdd(LabelKey(label(row)), guid);
        }
        return new ArticleGuids(map);
    }

    public Guid Keep(int? tierId, int category) =>
        existing.TryGetValue(TierKey(tierId, category), out var guid) ? guid : Guid.NewGuid();

    public Guid Keep(int id, string label) =>
        existing.TryGetValue(IdKey(id), out var byId) ? byId
        : existing.TryGetValue(LabelKey(label), out var byLabel) ? byLabel
        : Guid.NewGuid();

    private static string TierKey(int? tierId, int category) => $"tier:{tierId ?? 0}:{category}";

    private static string IdKey(int id) => $"id:{id}";

    private static string LabelKey(string label) => $"label:{label.Trim().ToLowerInvariant()}";
}
