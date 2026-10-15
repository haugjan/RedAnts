using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Halls;

public static class CreateHall
{
    public sealed record Command(string Id, string Name);

    public sealed class Handler(IHallRepository halls)
    {
        public async Task<Hall> HandleAsync(Command command)
        {
            var hall = Hall.Create(command.Id, command.Name);
            if (await halls.GetByIdAsync(hall.Id) is not null)
                throw new ValidationException(nameof(Hall.Id), $"Die Halle „{hall.Id}“ gibt es schon.");
            await halls.SaveAsync(hall);
            return hall;
        }
    }
}
