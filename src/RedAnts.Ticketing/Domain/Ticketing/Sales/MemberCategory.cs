namespace RedAnts.Domain.Ticketing.Sales;

public enum MemberCategory
{
    RedAnts,
    Block4
}

public static class MemberCategoryExtensions
{
    public static string DisplayName(this MemberCategory category) => category switch
    {
        MemberCategory.RedAnts => "Red Ants",
        MemberCategory.Block4 => "Block 4",
        _ => category.ToString()
    };
}
