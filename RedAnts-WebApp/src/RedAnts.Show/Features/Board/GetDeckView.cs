using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Board;

public static class GetDeckView
{
    public sealed record Query(string HallId, string DeckId);

    public sealed class Handler(IDeckMirror mirror, IHallRepository halls, ISounds sounds)
    {
        public async Task<DeckView?> HandleAsync(Query query)
        {
            var hall = await halls.GetByIdAsync(query.HallId);
            if (hall is null) return null;
            var layout = hall.DeckLayouts.FirstOrDefault(d => d.Id == query.DeckId) ?? hall.DeckLayouts[0];
            if (mirror.IsConnected(query.HallId) && await mirror.LiveAsync(query.HallId, layout) is { } live) return live;
            return Local(layout, await sounds.GetAllAsync());
        }

        private static DeckView Local(DeckLayout layout, IReadOnlyList<SoundCommand> sounds)
        {
            var keys = new List<DeckKeyRow>();
            for (var number = 1; number <= layout.Count; number++)
            {
                var key = layout.KeyAt(number);
                if (key is null || key.Kind == DeckKeyKinds.Empty)
                {
                    keys.Add(new DeckKeyRow(number, "", null, "#000000", "#ffffff", false, null));
                    continue;
                }
                var sound = key.Kind == DeckKeyKinds.Sound ? sounds.FirstOrDefault(s => s.Id == key.SoundId) : null;
                keys.Add(new DeckKeyRow(
                    number,
                    key.Label.Length > 0 ? key.Label : sound?.Label ?? "",
                    key.Icon,
                    key.Color.Length > 0 ? key.Color : "#333333",
                    "#ffffff",
                    sound is not null,
                    sound?.Id));
            }
            return new DeckView(layout.Id, layout.Name, layout.Columns, keys, false);
        }
    }
}
