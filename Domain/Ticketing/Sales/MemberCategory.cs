namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>The category a member card belongs to. Members are grouped by club membership, not by the
/// purchasable <see cref="TicketCategory"/> (which does not apply to them). Stored as its integer value.</summary>
public enum MemberCategory
{
    RedAnts,
    Block4
}

public static class MemberCategoryExtensions
{
    /// <summary>Public/display label for a member category.</summary>
    public static string DisplayName(this MemberCategory category) => category switch
    {
        MemberCategory.RedAnts => "Red Ants",
        MemberCategory.Block4 => "Block 4",
        _ => category.ToString()
    };
}
