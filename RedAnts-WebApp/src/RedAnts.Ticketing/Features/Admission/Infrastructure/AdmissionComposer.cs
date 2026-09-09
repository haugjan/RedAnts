using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Admission.Infrastructure;

public sealed class AdmissionComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IAdmissionRepository, AdmissionRepository>();
        builder.Services.AddScoped<IFreeEntryRepository, FreeEntryRepository>();
        builder.Services.AddScoped<IOccupancyReader, OccupancyReader>();
        builder.Services.AddScoped<IAdmissionFactsReader, AdmissionFactsReader>();
        builder.Services.AddScoped<ITicketRedemptions, TicketRedemptions>();
        builder.Services.AddScoped<IVisitLogReader, VisitLogReader>();
        builder.Services.AddScoped<IFreeEntryListReader, FreeEntryListReader>();
        builder.Services.AddScoped<IEventAdmissionReader, EventAdmissionReader>();
    }
}
