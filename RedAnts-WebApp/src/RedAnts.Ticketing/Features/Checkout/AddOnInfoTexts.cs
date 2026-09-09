using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Checkout;

internal static class AddOnInfoTexts
{
    public static async Task<List<string>> CollectAsync(ISeasonAddOnRepository seasonAddOns, OrderSnapshot snapshot)
    {
        var infos = new List<string>();
        foreach (var group in snapshot.AddOns.GroupBy(a => a.SeasonId))
        {
            var byId = (await seasonAddOns.GetBySeasonAsync(group.Key)).ToDictionary(a => a.Id);
            foreach (var addOn in group)
                if (byId.TryGetValue(addOn.Id, out var definition) && !string.IsNullOrWhiteSpace(definition.InfoAfterPurchase))
                    infos.Add(definition.InfoAfterPurchase!);
        }
        return infos.Distinct().ToList();
    }
}
