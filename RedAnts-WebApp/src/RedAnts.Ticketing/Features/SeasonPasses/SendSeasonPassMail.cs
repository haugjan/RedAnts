using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class SendSeasonPassMail
{
    public sealed record Command(Guid Uuid, string CategoryLabel, string? Email, string Subject, string Body);

    public sealed class Handler(ISeasonPassRepository passes, ISeasonPassMailer mailer)
    {
        public async Task<EmailSendResult> HandleAsync(Command command)
        {
            var pass = await passes.GetByUuidAsync(command.Uuid);
            if (pass is null) return new EmailSendResult(false, EditSeasonPass.NotFound);
            if (string.IsNullOrWhiteSpace(pass.Email)) pass.SetEmail(command.Email);
            return await mailer.SendAsync(pass, command.CategoryLabel, command.Subject, command.Body);
        }
    }
}
