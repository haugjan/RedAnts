using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Infrastructure.Ticketing;

/// <summary>
/// Season ticket prices (CHF) per category + age group. Reads overrides from the
/// "SeasonTicketPricing:Category:AgeGroup" config keys, otherwise uses defaults.
/// </summary>
public sealed class ConfigSeasonTicketPricing(IConfiguration config) : ISeasonTicketPricing
{
    private static readonly Dictionary<(SeasonTicketCategory, AgeGroup), decimal> Defaults = new()
    {
        [(SeasonTicketCategory.Block4, AgeGroup.Child)] = 40m,
        [(SeasonTicketCategory.Block4, AgeGroup.Youth)] = 60m,
        [(SeasonTicketCategory.Block4, AgeGroup.Adult)] = 80m,
        [(SeasonTicketCategory.Members, AgeGroup.Child)] = 60m,
        [(SeasonTicketCategory.Members, AgeGroup.Youth)] = 90m,
        [(SeasonTicketCategory.Members, AgeGroup.Adult)] = 120m,
        [(SeasonTicketCategory.Extern, AgeGroup.Child)] = 80m,
        [(SeasonTicketCategory.Extern, AgeGroup.Youth)] = 120m,
        [(SeasonTicketCategory.Extern, AgeGroup.Adult)] = 160m,
    };

    public decimal PriceFor(SeasonTicketCategory category, AgeGroup ageGroup)
    {
        var key = $"SeasonTicketPricing:{category}:{ageGroup}";
        if (decimal.TryParse(config[key], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var configured))
            return configured;
        return Defaults.TryGetValue((category, ageGroup), out var d) ? d : 0m;
    }

    public IReadOnlyList<(SeasonTicketCategory Category, AgeGroup AgeGroup, decimal Amount)> All() =>
        Enum.GetValues<SeasonTicketCategory>()
            .SelectMany(c => Enum.GetValues<AgeGroup>().Select(a => (c, a, PriceFor(c, a))))
            .ToList();
}
