using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Admin;

public static class CreateDemoSquads
{
    public const int MaxCount = 12;

    public sealed record Command(int Count);

    public sealed class Handler(IDemoSquads demo, IGameSettings settings)
    {
        public Task<int> HandleAsync(Command command)
        {
            if (command.Count < 1 || command.Count > MaxCount)
                throw new ValidationException(nameof(command.Count), $"Zwischen 1 und {MaxCount} Demokader.");

            return demo.CreateAsync(command.Count, settings.Budget);
        }
    }
}
