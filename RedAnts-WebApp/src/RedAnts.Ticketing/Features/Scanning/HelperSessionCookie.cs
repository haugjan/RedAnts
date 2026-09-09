using Microsoft.AspNetCore.DataProtection;

namespace RedAnts.Ticketing.Features.Scanning;

public static class HelperSessionCookie
{
    public const string Name = "RedAnts.Helper";
    public const string Purpose = "RedAnts.HelperSession.v1";

    public static string Protect(IDataProtectionProvider dataProtection, int helperId) =>
        dataProtection.CreateProtector(Purpose).Protect(helperId.ToString());
}
