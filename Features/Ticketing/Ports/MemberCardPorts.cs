using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

/// <summary>One parsed member-import row. Any field may be missing (null): rows with one or all fields
/// empty are still imported (as an anonymous/incomplete card).</summary>
public sealed record MemberImportRow(string? LastName, string? FirstName, DateOnly? Birthday);

/// <summary>Write side for member cards. Members are created by importing a CSV batch: all cards of an
/// import share the chosen season, category and the optional reference.</summary>
public interface IMemberCards
{
    /// <summary>Creates one member card per row; returns the number created.</summary>
    Task<int> ImportAsync(int seasonId, MemberCategory category, string? reference, IReadOnlyList<MemberImportRow> rows);
}
