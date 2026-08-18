using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;

namespace RedAnts.Infrastructure.Shared;

public sealed class BackOfficeAuthComposer : IComposer
{
    internal const string Scheme = "Umbraco.MicrosoftEntra";
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

        var scheme = Scheme;

        builder.Services.AddSingleton<IPackageManifestReader, MicrosoftEntraAuthProviderManifestReader>();

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
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage.Prompt = "select_account";
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            if (context.Principal?.Identity is ClaimsIdentity identity
                                && identity.FindFirst(ClaimTypes.Email) is null)
                            {
                                var email = context.Principal.FindFirstValue("email")
                                            ?? context.Principal.FindFirstValue("preferred_username")
                                            ?? context.Principal.FindFirstValue(ClaimTypes.Upn)
                                            ?? context.Principal.FindFirstValue("upn");
                                if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
                                    identity.AddClaim(new Claim(ClaimTypes.Email, email));
                            }
                            return Task.CompletedTask;
                        }
                    };
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

public sealed class MicrosoftEntraAuthProviderManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "RedAnts.BackOfficeAuth",
            AllowPublicAccess = true,
            Extensions =
            [
                new
                {
                    type = "authProvider",
                    alias = "RedAnts.AuthProviders.MicrosoftEntra",
                    name = "Microsoft Entra login provider",
                    forProviderName = BackOfficeAuthComposer.Scheme,
                    meta = new
                    {
                        label = "Microsoft",
                        defaultView = new
                        {
                            icon = "icon-cloud",
                            look = "primary",
                            color = "default"
                        },
                        linking = new
                        {
                            allowManualLinking = false
                        }
                    }
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>(new[] { manifest });
    }
}
