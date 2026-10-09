namespace RedAnts.Game.Domain;

public enum PlayerStatus
{
    Unknown = 0,
    Stays = 1,
    Missing = 2,
    New = 3,
    Transfer = 4,
    Returning = 5,
}

public static class PlayerStatusText
{
    public static string Label(this PlayerStatus status) => status switch
    {
        PlayerStatus.Stays => "bleibt",
        PlayerStatus.Missing => "fehlt",
        PlayerStatus.New => "neu",
        PlayerStatus.Transfer => "wechselt",
        PlayerStatus.Returning => "zurück",
        _ => "",
    };

    public static bool IsWorthShowing(this PlayerStatus status) =>
        status is PlayerStatus.Missing or PlayerStatus.New or PlayerStatus.Transfer;
}
