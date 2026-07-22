namespace RedAnts.Infrastructure.Shared;

public static class EmailLayout
{
    private const string Brand = "Red Ants Ticketing";
    private const string Accent = "#C8102E";

    private static readonly string LogoDataUri = LoadHeaderLogo();

    private static string LoadHeaderLogo()
    {
        try
        {
            foreach (var root in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var path = Path.Combine(root, "wwwroot", "img", "logo-header-mail.png");
                if (File.Exists(path))
                    return "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
            }
            return "";
        }
        catch { return ""; }
    }

    public static string Render(
        string title,
        string body,
        string? greeting = null,
        string? details = null,
        string? note = null,
        string? headerLogo = null)
    {
        var logo = string.IsNullOrEmpty(headerLogo) ? LogoDataUri : headerLogo;
        var bodyHtml = (body ?? "").Replace("\n", "<br>");
        var detailsHtml = string.IsNullOrWhiteSpace(details) ? null : details!.Replace("\n", "<br>");

        var greetingBlock = string.IsNullOrWhiteSpace(greeting)
            ? ""
            : $"<p style=\"margin:0 0 14px;font-size:16px;color:#101010;\">{greeting}</p>";

        var detailsBlock = detailsHtml is null
            ? ""
            : $"""
              <tr><td style="padding:22px 40px 0;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f5f5f5;border-left:4px solid {Accent};border-radius:4px;">
                  <tr><td style="padding:16px 18px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#323232;font-size:14px;line-height:1.6;">{detailsHtml}</td></tr>
                </table>
              </td></tr>
              """;

        var noteBlock = string.IsNullOrWhiteSpace(note)
            ? ""
            : $"""<tr><td style="padding:18px 40px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#666666;font-size:13px;line-height:1.6;">{note}</td></tr>""";

        return $"""
        <!DOCTYPE html>
        <html lang="de" xmlns="http://www.w3.org/1999/xhtml">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
        </head>
        <body style="margin:0;padding:0;background:#f4f4f4;">
          <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{title}</div>
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f4f4f4;">
            <tr><td align="center" style="padding:32px 12px;">
              <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px;background:#ffffff;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;">
                <tr><td style="height:6px;line-height:6px;font-size:6px;background:{Accent};">&nbsp;</td></tr>
                <tr><td align="center" style="padding:22px 40px 6px;">
                  {(string.IsNullOrEmpty(logo)
                      ? $"<span style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;color:{Accent};font-size:20px;\">Red Ants</span>"
                      : $"<img src=\"{logo}\" alt=\"Red Ants Winterthur\" width=\"46\" height=\"41\" style=\"width:46px;height:41px;max-width:46px;display:block;border:0;margin:0 auto;\">")}
                </td></tr>
                <tr><td style="padding:14px 40px 0;">
                  <h1 style="margin:0;font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;color:#101010;font-size:26px;line-height:1.15;">{title}</h1>
                  <div style="height:3px;width:48px;background:{Accent};margin-top:14px;"></div>
                </td></tr>
                <tr><td style="padding:18px 40px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#323232;font-size:15px;line-height:1.65;">
                  {greetingBlock}
                  <div>{bodyHtml}</div>
                </td></tr>
                {detailsBlock}
                {noteBlock}
                <tr><td style="padding:34px 0 0;">&nbsp;</td></tr>
                <tr><td style="border-top:1px solid #f0f0f0;font-size:0;line-height:0;">&nbsp;</td></tr>
                <tr><td align="center" style="padding:22px 40px 32px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#9ca3af;font-size:13px;line-height:1.7;">
                  <span style="font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;color:#101010;font-size:15px;">{Brand}</span>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
