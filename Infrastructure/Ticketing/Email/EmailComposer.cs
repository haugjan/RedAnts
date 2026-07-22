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
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<IHelperInviteMailer, HelperInviteMailer>();
        builder.Services.AddUnique<Umbraco.Cms.Core.Mail.IEmailSender, UmbracoEmailSenderAdapter>();
    }
}
