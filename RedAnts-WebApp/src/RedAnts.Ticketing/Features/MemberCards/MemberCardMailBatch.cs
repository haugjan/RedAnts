using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.MemberCards;

public sealed record MemberCardMailBatch(string Reference, MemberCategory Category, IReadOnlyList<MemberCardRow> Cards)
{
    public string Key => $"{Reference}|{(int)Category}";
    public string Label => $"{Reference} – {Category.DisplayName()}";
    public int WithEmail => Cards.Count(c => c.HasEmail);
    public int WithoutEmail => Cards.Count(c => !c.HasEmail);
}
