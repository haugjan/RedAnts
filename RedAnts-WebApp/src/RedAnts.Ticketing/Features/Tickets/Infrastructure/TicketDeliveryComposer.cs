using QuestPDF.Infrastructure;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class TicketDeliveryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        builder.Services.AddSingleton<ITicketPdf, TicketPdfRenderer>();
    }
}
