using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.MemberCards.Infrastructure;

public sealed class MemberCardsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IMemberCardRepository, MemberCardRepository>();
        builder.Services.AddScoped<IMemberCardListReader, MemberCardListReader>();
    }
}
