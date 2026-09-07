using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

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
