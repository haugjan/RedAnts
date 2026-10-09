using RedAnts.Game.Domain;

namespace RedAnts.Game.Features.Shared;

public sealed record PlayerCardView(
    int PlayerId,
    string FirstName,
    string LastName,
    PlayerPosition Position,
    string Club,
    string Number,
    string? PortraitUrl,
    int MarketValue,
    decimal RawAverage,
    int Goals,
    int Assists,
    int Games,
    PlayerStatus Status)
{
    public string FullName => string.IsNullOrEmpty(FirstName) ? LastName : $"{FirstName} {LastName}";
}

public static class PlayerName
{
    public static (string FirstName, string LastName) Split(string? name)
    {
        var cleaned = (name ?? "").Trim();
        if (cleaned.Length == 0) return ("", "");
        var cut = cleaned.LastIndexOf(' ');
        return cut <= 0 ? ("", cleaned) : (cleaned[..cut], cleaned[(cut + 1)..]);
    }
}
