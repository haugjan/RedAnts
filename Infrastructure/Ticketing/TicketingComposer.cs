using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Purchase;
using RedAnts.Infrastructure.Shared;
using RedAnts.Infrastructure.Ticketing.Content;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Infrastructure.Ticketing;

public class TicketingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Schema migration (creates the ticketing tables at boot)
        builder.AddComponent<TicketingMigrationComponent>();

        // HTTP clients for external services
        builder.Services.AddHttpClient("Brevo");
        builder.Services.AddHttpClient("Turnstile");
        builder.Services.AddHttpClient("Payrexx");

        // Catalog read-adapters (Umbraco Document Types via the published cache)
        builder.Services.AddScoped<ISeasons, UmbracoSeasons>();
        builder.Services.AddScoped<IVenues, UmbracoVenues>();
        builder.Services.AddScoped<IEvents, UmbracoEvents>();

        // Ticket repositories (NPoco)
        builder.Services.AddScoped<ISingleTickets, SingleTicketRepository>();
        builder.Services.AddScoped<ISeasonTickets, SeasonTicketRepository>();
        builder.Services.AddScoped<IMemberCards, MemberCardRepository>();
        builder.Services.AddScoped<ITicketScanLog, TicketScanLogRepository>();

        // Cross-cutting adapters
        builder.Services.AddSingleton<ISqidEncoder, SqidEncoder>();
        builder.Services.AddScoped<ICaptcha, TurnstileTicketCaptcha>();
        builder.Services.AddScoped<IPaymentGateway, PayrexxPaymentGateway>();
        builder.Services.AddScoped<ITicketEmail, BrevoTicketEmail>();
        builder.Services.AddSingleton<ISeasonTicketPricing, ConfigSeasonTicketPricing>();

        // Use cases
        builder.Services.AddScoped<StartSingleTicketPurchase>();
        builder.Services.AddScoped<StartSeasonTicketPurchase>();
        builder.Services.AddScoped<CompletePurchase>();
    }
}
