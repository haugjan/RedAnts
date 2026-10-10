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
                    throw new ValidationException(GameSettingKeys.Budget, "Das Budget liegt zwischen 100 und 100000 Floorbucks.");
                return settings.SetAsync(key, budget.ToString());
            }

            if (key == GameSettingKeys.Round)
            {
                if (!int.TryParse(command.Value, out var round) || round < 1 || round > 200)
                    throw new ValidationException(GameSettingKeys.Round, "Die Runde liegt zwischen 1 und 200.");
                return settings.SetAsync(key, round.ToString());
            }

            return settings.SetAsync(key, command.Value);
        }
    }
}
