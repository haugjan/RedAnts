using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Board;

public sealed record DJTileSummary(string Id, string Label, string? Icon, string? Color, string ProfileId, string ProfileName, string Path);

public static class GetDJTiles
{
    public sealed record Query;

    public sealed class Handler(IDJProfiles profiles)
    {
        public async Task<IReadOnlyList<DJTileSummary>> HandleAsync(Query query)
        {
            var tiles = new List<DJTileSummary>();
            foreach (var profile in await profiles.GetAllAsync())
                Collect(profile, profile.Root, "", tiles);
            return tiles;
        }

        private static void Collect(DJProfile profile, IReadOnlyList<DJButton> nodes, string path, List<DJTileSummary> tiles)
        {
            foreach (var node in nodes)
            {
                if (node.IsFolder)
                {
                    Collect(profile, node.Children!, path.Length == 0 ? node.Label : path + " / " + node.Label, tiles);
                    continue;
                }
                if (node.IsControl || node.EffectiveSongs.Count == 0) continue;
                tiles.Add(new DJTileSummary(node.Id, node.Label, node.Icon, node.Color, profile.Id, profile.Name, path));
            }
        }
    }
}
