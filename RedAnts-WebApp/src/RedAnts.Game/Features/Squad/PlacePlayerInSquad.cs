using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players;
using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Squad;

public static class PlacePlayerInSquad
{
    public sealed record Command(string ManagerToken, int Slot, int PlayerId);

    public sealed class Handler(ISquadRepository squads, IGamePlayerRepository players, IGameSettings settings)
    {
        public async Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.ManagerToken))
                throw new DomainException("Kein Managerkennzeichen.");

            var player = await players.GetByExternalIdAsync(command.PlayerId)
                ?? throw new DomainException("Diese Spielerin gibt es nicht.");

            var squad = await squads.GetByManagerTokenAsync(command.ManagerToken)
                ?? Domain.Squad.Create(command.ManagerToken, SwissTime.Timestamp);

            squad.Place(command.Slot, player.Position, new SquadPick(player.ExternalId, player.MarketValue), settings.Budget);

            await squads.SaveAsync(squad);
        }
    }
}
