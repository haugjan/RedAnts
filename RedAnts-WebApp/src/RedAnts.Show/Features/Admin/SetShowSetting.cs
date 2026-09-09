
namespace RedAnts.Show.Features.Admin;

public static class SetShowSetting
{
    public sealed record Command(string Key, string? Value);

    public sealed class Handler(IShowSettings settings)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Key)) throw new DomainException("Ein Einstellungsschlüssel ist erforderlich.");
            return settings.SetAsync(command.Key.Trim(), command.Value);
        }
    }
}
