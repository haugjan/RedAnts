namespace RedAnts.Infrastructure.Ticketing.Email;

public static class MailTicketActions
{
    public static string Render(string openUrl, string openLabel, string shareTitle)
    {
        var whatsapp = "https://wa.me/?text=" + Uri.EscapeDataString($"{shareTitle}\n{openUrl}");
        return
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 auto 40px;max-width:420px;\">" +
                Button(openUrl, "#D02D38", openLabel, "8px", newTab: false) +
                Button($"{openUrl}/pdf", "#14171A", "Als PDF herunterladen", "8px", newTab: false) +
                Button(whatsapp, "#25D366", "Per WhatsApp teilen", "0", newTab: true) +
            "</table>";
    }

    private static string Button(string href, string background, string label, string paddingBottom, bool newTab)
    {
        var target = newTab ? " target=\"_blank\" rel=\"noopener\"" : "";
        return
            $"<tr><td style=\"padding:0 0 {paddingBottom};\">" +
                $"<a href=\"{href}\"{target} style=\"display:block;text-align:center;background:{background};color:#ffffff;text-decoration:none;font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;font-size:15px;padding:13px 18px;border-radius:10px;\">{label}</a>" +
            "</td></tr>";
    }
}
