namespace RedAnts.Game.Features.Squad;

public static class RenameSquad
{
    public sealed record Command(string ManagerToken, string Name);

    public sealed class Handler(ISquadRepository squads)
    {
        public async Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.ManagerToken))
                throw new DomainException("Kein Managerkennzeichen.");

            var squad = await squads.GetByManagerTokenAsync(command.ManagerToken)
                ?? Domain.Squad.Create(command.ManagerToken, SwissTime.Timestamp);

            squad.Rename(command.Name);
            await squads.SaveAsync(squad);
        }
    }
}
