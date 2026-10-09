namespace RedAnts.Game.Features.Squad;

public static class ClearSquadSlot
{
    public sealed record Command(string ManagerToken, int Slot);

    public sealed class Handler(ISquadRepository squads)
    {
        public async Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.ManagerToken))
                throw new DomainException("Kein Managerkennzeichen.");

            var squad = await squads.GetByManagerTokenAsync(command.ManagerToken);
            if (squad is null) return;

            squad.Clear(command.Slot);
            await squads.SaveAsync(squad);
        }
    }
}
