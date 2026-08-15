using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace RedAnts.Infrastructure.Shared;

public sealed class BackOfficeAuthComposer : IComposer
{
    private const string EmailDomain = "@redants.ch";

    public void Compose(IUmbracoBuilder builder)
    {
        var tenantId = builder.Config["BackOfficeAuth:TenantId"];
        var clientId = builder.Config["BackOfficeAuth:ClientId"];
        var clientSecret = builder.Config["BackOfficeAuth:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            return;
        }

        var scheme = Constants.Security.BackOfficeExternalAuthenticationTypePrefix + "MicrosoftEntra";

        builder.AddBackOfficeExternalLogins(logins =>
            logins.AddBackOfficeLogin(
                auth => auth.AddOpenIdConnect(scheme, "Microsoft (@redants.ch)", options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.ResponseType = "code";
                    options.UsePkce = true;
                    options.CallbackPath = "/umbraco-entra-signin";
                    options.SignedOutCallbackPath = "/umbraco-entra-signout";
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.TokenValidationParameters.NameClaimType = "preferred_username";
                }),
                providerOptions =>
                {
                    providerOptions.DenyLocalLogin = true;
                    providerOptions.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
                        autoLinkExternalAccount: true,
                        defaultUserGroups: [],
                        allowManualLinking: false)
                    {
                        OnAutoLinking = (user, _) =>
                        {
                            if (!user.HasIdentity)
                            {
                                throw new InvalidOperationException(
                                    "Kein bestehender Umbraco-Benutzer mit dieser Adresse; automatische Anlage ist deaktiviert.");
                            }
                        },
                        OnExternalLogin = (_, loginInfo) => IsRedAntsAddress(loginInfo),
                    };
                }));
    }

    private static bool IsRedAntsAddress(ExternalLoginInfo loginInfo)
    {
        var identifier = loginInfo.Principal.FindFirstValue(ClaimTypes.Email)
                         ?? loginInfo.Principal.FindFirstValue("preferred_username")
                         ?? loginInfo.Principal.FindFirstValue("upn");
        return identifier is not null
               && identifier.Trim().EndsWith(EmailDomain, StringComparison.OrdinalIgnoreCase);
    }
}
