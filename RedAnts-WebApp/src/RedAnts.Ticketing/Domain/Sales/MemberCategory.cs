namespace RedAnts.Ticketing.Domain.Sales;

public enum MemberCategory
{
    RedAnts,
    Block4,
    Company
}

public static class MemberCategoryExtensions
{
    public static string DisplayName(this MemberCategory category) => category switch
    {
        MemberCategory.RedAnts => "Red Ants",
        MemberCategory.Block4 => "Block 4 einzel",
        MemberCategory.Company => "Block 4 Firma",
        _ => category.ToString()
    };

    public static bool IsBlock4(this MemberCategory category) =>
        category is MemberCategory.Block4 or MemberCategory.Company;
}
