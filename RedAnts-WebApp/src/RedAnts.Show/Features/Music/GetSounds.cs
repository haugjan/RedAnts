using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public static class GetSounds
{
    public sealed record Query;

    public sealed class Handler(ISounds sounds)
    {
        public Task<IReadOnlyList<SoundCommand>> HandleAsync(Query query) => sounds.GetAllAsync();
    }
}
