using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public static class LayoutEdits
{
    public static IEnumerable<int> Footprint(NumberedRun run) =>
        run.Buttons.Count == 0 ? [run.Start] : run.Span;

    public static Placement? PlacementAt(BoardLayout layout, int number) =>
        layout.Placements.FirstOrDefault(p => p.Number == number);

    public static NumberedRun? RunAt(BoardLayout layout, int number) =>
        layout.Sequences.FirstOrDefault(r => Footprint(r).Contains(number));

    public static NumberedRun? Run(BoardLayout layout, string? id) =>
        layout.Sequences.FirstOrDefault(r => r.Id == id);

    public static int NextSlot(NumberedRun run) => run.Start + run.Buttons.Count;

    public static bool Claimed(BoardLayout layout, int number, string? exceptRun = null) =>
        PlacementAt(layout, number) is not null
        || layout.Sequences.Any(r => r.Id != exceptRun && Footprint(r).Contains(number));

    public static bool IsFree(BoardLayout layout, BoardDeck deck, int number, BoardLayout? behind = null, string? exceptRun = null) =>
        number >= 1 && number <= deck.Count
        && !deck.Reserved(number)
        && !Claimed(layout, number, exceptRun)
        && (behind is null || !Claimed(behind, number));

    public static IReadOnlyList<LayoutSlot> Slots(TabBoard board, BoardDeck deck, BoardLayout layout, BoardLayout? behind = null)
    {
        var slots = new List<LayoutSlot>();
        for (var number = 1; number <= deck.Count; number++)
            slots.Add(SlotOf(board, deck, layout, behind, number));
        return slots;
    }

    private static LayoutSlot SlotOf(TabBoard board, BoardDeck deck, BoardLayout layout, BoardLayout? behind, int number)
    {
        if (deck.Reserved(number))
            return new LayoutSlot(number, LayoutSlotKind.Reserved, Caption: number == deck.Panic ? "PANIC" : "Zurück");
        if (Occupied(board, layout, number, LayoutSlotKind.Placed, LayoutSlotKind.Run) is { } own)
            return own;
        if (behind is not null && Occupied(board, behind, number, LayoutSlotKind.Always, LayoutSlotKind.Always) is { } always)
            return always;
        return new LayoutSlot(number, LayoutSlotKind.Free);
    }

    private static LayoutSlot? Occupied(TabBoard board, BoardLayout layout, int number, LayoutSlotKind placed, LayoutSlotKind inRun)
    {
        if (PlacementAt(layout, number) is { } placement)
            return new LayoutSlot(number, placed, board.Button(placement.Button), Missing(board, placement.Button));
        if (RunAt(layout, number) is not { } run)
            return null;
        if (run.Buttons.Count == 0)
            return new LayoutSlot(number, inRun, Caption: $"{run.Name} (leer)", RunId: run.Id);
        var index = number - run.Start;
        var id = run.Buttons[index];
        return new LayoutSlot(number, inRun, board.Button(id), Missing(board, id), index + 1, run.Id);
    }

    private static string? Missing(TabBoard board, string id) =>
        board.Button(id) is null ? $"fehlt: {id}" : null;

    public static IReadOnlyList<LayoutSlot> Preview(TabBoard board, BoardDeck deck, string tab, string league)
    {
        var keys = board.Resolve(deck, tab, league).ToDictionary(k => k.Number);
        var slots = new List<LayoutSlot>();
        for (var number = 1; number <= deck.Count; number++)
        {
            if (deck.Reserved(number))
                slots.Add(new LayoutSlot(number, LayoutSlotKind.Reserved, Caption: number == deck.Panic ? "PANIC" : "Zurück"));
            else if (keys.TryGetValue(number, out var key))
                slots.Add(new LayoutSlot(number, key.Position is null ? LayoutSlotKind.Placed : LayoutSlotKind.Run,
                    key.Button, Position: key.Position));
            else
                slots.Add(new LayoutSlot(number, LayoutSlotKind.Free));
        }
        return slots;
    }

    public static BoardLayout Place(BoardLayout layout, BoardDeck deck, int number, string button, BoardLayout? behind = null)
    {
        if (PlacementAt(layout, number) is { } existing)
            return With(layout, layout.Placements.Select(p => p.Number == number ? existing with { Button = button } : p));
        Require(IsFree(layout, deck, number, behind), $"Platz {number} ist nicht frei.");
        return With(layout, layout.Placements.Append(new Placement(number, button)));
    }

    public static BoardLayout Remove(BoardLayout layout, int number) =>
        With(layout, layout.Placements.Where(p => p.Number != number));

    public static BoardLayout Move(BoardLayout layout, BoardDeck deck, int from, int to, BoardLayout? behind = null)
    {
        Require(PlacementAt(layout, from) is not null, $"Auf Platz {from} liegt kein einzelner Button.");
        Require(IsFree(layout, deck, to, behind), $"Platz {to} ist nicht frei.");
        return With(layout, layout.Placements.Select(p => p.Number == from ? p with { Number = to } : p));
    }

    public static (BoardLayout Layout, string RunId) AddRun(BoardLayout layout, BoardDeck deck, string name, int start, BoardLayout? behind = null)
    {
        var title = name.Trim();
        Require(title.Length > 0, "Bitte einen Namen für die Reihe angeben.");
        Require(IsFree(layout, deck, start, behind), $"Platz {start} ist nicht frei, die Reihe braucht einen freien Startplatz.");
        var id = ButtonIds.Next(title, layout.Sequences.Select(r => r.Id));
        return (With(layout, runs: layout.Sequences.Append(new NumberedRun(id, title, start, []))), id);
    }

    public static BoardLayout AddToRun(BoardLayout layout, BoardDeck deck, string runId, string button, BoardLayout? behind = null)
    {
        var run = Existing(layout, runId);
        if (run.Buttons.Count > 0)
        {
            var next = run.Start + run.Buttons.Count;
            Require(IsFree(layout, deck, next, behind, runId),
                next > deck.Count
                    ? $"Reihe „{run.Name}“ reicht schon bis ans Ende des Rasters."
                    : $"Reihe „{run.Name}“ kann nicht wachsen: Platz {next} ist belegt.");
        }
        return Change(layout, runId, r => r with { Buttons = [.. r.Buttons, button] });
    }

    public static BoardLayout RemoveFromRun(BoardLayout layout, string runId, int index) =>
        Change(layout, runId, r => index < 0 || index >= r.Buttons.Count
            ? r
            : r with { Buttons = r.Buttons.Where((_, i) => i != index).ToList() });

    public static BoardLayout Shift(BoardLayout layout, string runId, int index, int delta) =>
        Change(layout, runId, r =>
        {
            var target = index + delta;
            if (index < 0 || index >= r.Buttons.Count || target < 0 || target >= r.Buttons.Count) return r;
            var buttons = r.Buttons.ToList();
            (buttons[index], buttons[target]) = (buttons[target], buttons[index]);
            return r with { Buttons = buttons };
        });

    public static BoardLayout RenameRun(BoardLayout layout, string runId, string name)
    {
        var title = name.Trim();
        Require(title.Length > 0, "Bitte einen Namen für die Reihe angeben.");
        return Change(layout, runId, r => r with { Name = title });
    }

    public static BoardLayout MoveRun(BoardLayout layout, BoardDeck deck, string runId, int start, BoardLayout? behind = null)
    {
        var run = Existing(layout, runId);
        Require(start >= 1 && start <= deck.Count, $"Platz {start} liegt ausserhalb des Rasters.");
        var moved = run with { Start = start };
        var blocked = Footprint(moved).FirstOrDefault(n => !IsFree(layout, deck, n, behind, runId));
        Require(blocked == 0, blocked > deck.Count
            ? $"Ab Platz {start} passt die Reihe „{run.Name}“ nicht mehr ins Raster."
            : $"Ab Platz {start} stösst die Reihe „{run.Name}“ auf Platz {blocked}, der belegt ist.");
        return Change(layout, runId, _ => moved);
    }

    public static BoardLayout DeleteRun(BoardLayout layout, string runId) =>
        With(layout, runs: layout.Sequences.Where(r => r.Id != runId));

    public static bool IsEmpty(BoardLayout layout) => layout.Placements.Count == 0 && layout.Sequences.Count == 0;

    private static NumberedRun Existing(BoardLayout layout, string runId) =>
        Run(layout, runId) ?? throw new ValidationException(nameof(NumberedRun.Id), "Diese Reihe gibt es nicht mehr.");

    private static BoardLayout Change(BoardLayout layout, string runId, Func<NumberedRun, NumberedRun> change) =>
        With(layout, runs: layout.Sequences.Select(r => r.Id == runId ? change(r) : r));

    private static BoardLayout With(BoardLayout layout, IEnumerable<Placement>? keys = null, IEnumerable<NumberedRun>? runs = null) =>
        new((keys ?? layout.Placements).OrderBy(p => p.Number).ToList(), (runs ?? layout.Sequences).ToList());

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new ValidationException(nameof(Placement.Number), message);
    }
}
