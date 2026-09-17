using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Sounds;

namespace RedAnts.Show.Features.Admin;

public static class DeleteUnusedSounds
{
    public sealed record Command(IReadOnlyList<string> Paths);

    public sealed record Result(int Deleted, long FreedBytes, int Skipped);

    public sealed class Handler(IShowProfiles profiles, IShowSoundUploader uploader)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var referenced = ShowSoundReferences.Collect(await profiles.GetAllAsync());
            var sizes = (await uploader.ListAsync("sounds/")).ToDictionary(b => b.Path, b => b.SizeBytes, StringComparer.OrdinalIgnoreCase);

            var deleted = 0;
            var skipped = 0;
            long freed = 0;
            foreach (var path in command.Paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!path.StartsWith("sounds/", StringComparison.OrdinalIgnoreCase) || referenced.Contains(path))
                {
                    skipped++;
                    continue;
                }
                if (await uploader.DeleteAsync(path))
                {
                    deleted++;
                    if (sizes.TryGetValue(path, out var size)) freed += size;
                }
            }
            return new Result(deleted, freed, skipped);
        }
    }
}
