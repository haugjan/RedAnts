
namespace RedAnts.DJ.Features.Admin;

public static class SetDJSetting
{
    public sealed record Command(string Key, string? Value);

    public sealed class Handler(IDJSettings settings)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Key)) throw new DomainException("Ein Einstellungsschlüssel ist erforderlich.");
            return settings.SetAsync(command.Key.Trim(), command.Value);
        }
    }
}
