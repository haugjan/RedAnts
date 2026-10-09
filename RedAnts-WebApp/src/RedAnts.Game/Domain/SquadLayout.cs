namespace RedAnts.Game.Domain;

public static class SquadLayout
{
    public const int Goalies = 2;
    public const int Lines = 4;
    public const int DefencePerLine = 2;
    public const int ForwardsPerLine = 3;
    public const int PerLine = DefencePerLine + ForwardsPerLine;
    public const int Slots = Goalies + Lines * PerLine;

    public static bool IsSlot(int slot) => slot >= 0 && slot < Slots;

    public static PlayerPosition RoleOf(int slot)
    {
        Guard(slot);
        if (slot < Goalies) return PlayerPosition.Goalie;
        var inLine = (slot - Goalies) % PerLine;
        return inLine < DefencePerLine ? PlayerPosition.Defence : PlayerPosition.Forward;
    }

    public static int LineOf(int slot)
    {
        Guard(slot);
        return slot < Goalies ? 0 : (slot - Goalies) / PerLine + 1;
    }

    public static int PositionInLine(int slot)
    {
        Guard(slot);
        if (slot < Goalies) return slot + 1;
        var inLine = (slot - Goalies) % PerLine;
        return inLine < DefencePerLine ? inLine + 1 : inLine - DefencePerLine + 1;
    }

    public static string LabelOf(int slot)
    {
        Guard(slot);
        var role = RoleOf(slot);
        return role == PlayerPosition.Goalie
            ? $"Goalie {PositionInLine(slot)}"
            : $"{role.Label()} {PositionInLine(slot)}";
    }

    public static IReadOnlyList<int> SlotsOfLine(int line) =>
        line == 0
            ? Enumerable.Range(0, Goalies).ToArray()
            : Enumerable.Range(Goalies + (line - 1) * PerLine, PerLine).ToArray();

    private static void Guard(int slot)
    {
        if (!IsSlot(slot)) throw new DomainException($"Platz {slot} gibt es im Kader nicht.");
    }
}
