using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Shared;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Email;

public static class SendTestMail
{
    public sealed record Command(string To, Guid? TicketUuid);

    public sealed class Handler(
        IEmailSender email,
        IIssuedTicketReader tickets,
        ITicketTokens tokens,
        IQrCodeRenderer qr,
        IEventReader events,
        ISeasonReader seasons,
        IPublicBaseUrl publicUrl)
    {
        private const string Note = "Testmail aus der RedAnts-Entwicklungsumgebung.";

        public async Task<EmailSendResult> HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.To)) throw new DomainException("Eine Empfängeradresse ist erforderlich.");
            var (subject, html) = command.TicketUuid is Guid ticketId && await tickets.FindAsync(ticketId) is { } issued
                ? await TicketMailAsync(issued)
                : PlainMail();
            return await email.SendAsync(command.To, null, subject, html);
        }

        private async Task<(string Subject, string Html)> TicketMailAsync(IssuedTicket issued)
        {
            var url = publicUrl.TicketUrl(tokens.CreateShort(issued.Uuid));
            var qrPng = qr.RenderPngDataUri(url);
            var scopeName = issued.Type == TicketType.EventTicket
                ? (await events.FindByIdAsync(issued.ScopeId))?.Name ?? "Anlass"
                : (await seasons.FindByIdAsync(issued.ScopeId))?.Name ?? "Saison";
            var subject = $"Dein Ticket – {scopeName}";
            var body =
                $"Hier ist dein Ticket für <strong>{scopeName}</strong>.\nZeige den QR-Code am Eingang.\n\n" +
                $"<div style=\"text-align:center;margin:16px 0;\"><img src=\"{qrPng}\" alt=\"Ticket QR\" width=\"220\" height=\"220\" style=\"border:1px solid #eee;border-radius:8px;\"></div>\n" +
                $"Web-Ticket: <a href=\"{url}\">{url}</a>";
            return (subject, EmailLayout.Render(subject, body, greeting: "Hallo,", note: Note));
        }

        private static (string Subject, string Html) PlainMail()
        {
            const string subject = "Testmail – Red Ants";
            return (subject, EmailLayout.Render(subject,
                "Diese Testmail bestätigt, dass der Mailversand aus RedAnts funktioniert.",
                greeting: "Hallo,", note: Note));
        }
    }
}
