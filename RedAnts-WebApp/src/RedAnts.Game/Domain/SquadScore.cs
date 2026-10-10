namespace RedAnts.Game.Domain;

public static class SquadScore
{
    public static int PointsInRound(IReadOnlyList<int> form, int round) =>
        round >= 1 && round <= form.Count ? form[round - 1] : 0;

    public static int RoundPoints(IEnumerable<IReadOnlyList<int>> forms, int round) =>
        forms.Sum(form => PointsInRound(form, round));

    public static int TotalPoints(IEnumerable<IReadOnlyList<int>> forms, int round) =>
        round < 1 ? 0 : forms.Sum(form => form.Take(round).Sum());

    public static IReadOnlyList<int> PointsByRound(IEnumerable<IReadOnlyList<int>> forms, int round)
    {
        if (round < 1) return [];
        var material = forms.ToList();
        return Enumerable.Range(1, round).Select(r => RoundPoints(material, r)).ToList();
    }

    public static int RoundsPlayed(IEnumerable<IReadOnlyList<int>> forms)
    {
        var longest = 0;
        foreach (var form in forms)
            if (form.Count > longest) longest = form.Count;
        return longest;
    }
}
