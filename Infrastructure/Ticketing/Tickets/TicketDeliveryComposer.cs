using QuestPDF.Infrastructure;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketDeliveryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        builder.Services.AddSingleton<ITicketPdf, TicketPdfRenderer>();
    }
}
