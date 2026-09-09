using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Helpers.Infrastructure;

public sealed class HelpersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IHelperRepository, HelperMemberRepository>();
        builder.Services.AddScoped<IHelperListReader, HelperListReader>();
        builder.Services.AddScoped<IHelperScanReportReader, HelperScanReportReader>();
    }
}
