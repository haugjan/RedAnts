using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Email;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace RedAnts.Infrastructure.Ticketing.Email;

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

        builder.Services.AddScoped<IHelperInviteMailer, HelperInviteMailer>();
        builder.Services.AddScoped<IMemberCardMailer, MemberCardMailer>();
        builder.Services.AddUnique<Umbraco.Cms.Core.Mail.IEmailSender, UmbracoEmailSenderAdapter>();
    }
}
