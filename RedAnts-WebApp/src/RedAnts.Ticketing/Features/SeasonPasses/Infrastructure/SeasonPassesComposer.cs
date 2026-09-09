using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.SeasonPasses.Infrastructure;

public sealed class SeasonPassesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ISeasonPassRepository, SeasonPassRepository>();
        builder.Services.AddScoped<ISeasonPassListReader, SeasonPassListReader>();
        builder.Services.AddScoped<ISeasonPassPricing, SeasonPassPricingReader>();
    }
}
