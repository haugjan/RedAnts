using Microsoft.Extensions.DependencyInjection;
using RedAnts.Ticketing.Features.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Ticketing.Infrastructure.Scanning;

public sealed class AdmissionComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IAdmissionRepository, AdmissionRepository>();
        builder.Services.AddScoped<IFreeEntryRepository, FreeEntryRepository>();
        builder.Services.AddScoped<IOccupancyReader, OccupancyReader>();
        builder.Services.AddScoped<IAdmissionFactsReader, AdmissionFactsReader>();
        builder.Services.AddScoped<ITicketRedemptions, TicketRedemptions>();
    }
}
