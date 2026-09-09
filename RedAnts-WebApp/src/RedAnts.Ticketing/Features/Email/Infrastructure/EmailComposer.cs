using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.FlexTickets.Infrastructure;
using RedAnts.Ticketing.Features.Helpers;
using RedAnts.Ticketing.Features.Helpers.Infrastructure;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.MemberCards.Infrastructure;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.SeasonPasses.Infrastructure;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Email.Infrastructure;

public sealed class EmailComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<OutboxSignal>();
        builder.Services.AddScoped<IEmailSender, OutboxEnqueuer>();

        builder.Services.AddScoped<OutboxRepository>();
        builder.Services.AddScoped<IEmailOutbox>(sp => sp.GetRequiredService<OutboxRepository>());
        builder.Services.AddScoped<IOutboxAdminReport>(sp => sp.GetRequiredService<OutboxRepository>());

        builder.Services.AddScoped<IEmailTransport, GraphEmailTransport>();
        builder.Services.AddScoped<EmailTransportSelector>();
        builder.Services.AddHostedService<OutboxDispatcher>();

        builder.Services.AddScoped<ITicketingMailSettings, RedAnts.Ticketing.Features.Email.Infrastructure.TicketingMailSettings>();
        builder.Services.AddScoped<IHelperInviteMailer, HelperInviteMailer>();
        builder.Services.AddScoped<IMemberCardMailer, MemberCardMailer>();
        builder.Services.AddScoped<ISeasonPassMailer, SeasonPassMailer>();
        builder.Services.AddScoped<IEventTicketMailer, EventTicketMailer>();
        builder.Services.AddScoped<IFlexTicketMailer, FlexTicketMailer>();
        builder.Services.AddUnique<Umbraco.Cms.Core.Mail.IEmailSender, UmbracoEmailBridge>();
    }
}
