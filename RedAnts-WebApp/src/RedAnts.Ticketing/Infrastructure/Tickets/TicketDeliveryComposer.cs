using QuestPDF.Infrastructure;
using RedAnts.Ticketing.Features.Tickets;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Infrastructure.Tickets;

public sealed class TicketDeliveryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        builder.Services.AddSingleton<ITicketPdf, TicketPdfRenderer>();
    }
}
