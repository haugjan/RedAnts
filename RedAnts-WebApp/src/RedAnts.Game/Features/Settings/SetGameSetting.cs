using RedAnts.Game.Domain;

namespace RedAnts.Game.Features.Settings;

public static class SetGameSetting
{
    public sealed record Command(string Key, string? Value);

    public sealed class Handler(IGameSettings settings)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.Key))
                throw new DomainException("Ein Einstellungsschlüssel ist erforderlich.");

            var key = command.Key.Trim();
            if (key == GameSettingKeys.Budget)
            {
                if (!int.TryParse(command.Value, out var budget) || budget < 100 || budget > 100000)
                    throw new ValidationException(GameSettingKeys.Budget, "Das Budget liegt zwischen 100 und 100000 FB.");
                return settings.SetAsync(key, budget.ToString());
            }

            return settings.SetAsync(key, command.Value);
        }
    }
}
