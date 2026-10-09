namespace RedAnts.Game.Domain;

public enum PlayerPosition
{
    Goalie = 0,
    Defence = 1,
    Forward = 2,
}

public static class PlayerPositionText
{
    public static string Label(this PlayerPosition position) => position switch
    {
        PlayerPosition.Goalie => "Goalie",
        PlayerPosition.Defence => "Verteidigung",
        _ => "Sturm",
    };

    public static string ShortLabel(this PlayerPosition position) => position switch
    {
        PlayerPosition.Goalie => "G",
        PlayerPosition.Defence => "V",
        _ => "S",
    };
}
