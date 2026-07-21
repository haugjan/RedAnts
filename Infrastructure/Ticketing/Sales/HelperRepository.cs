using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class HelperRepository(IScopeProvider scopeProvider) : IHelpers
{
    public async Task<IReadOnlyList<Helper>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<HelperRecord>(
            "WHERE SeasonId = @0 ORDER BY Active DESC, LastName, FirstName, Id", seasonId);
        return rows.Select(Map).ToList();
    }

    public async Task<Helper?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.SingleOrDefaultByIdAsync<HelperRecord>(id);
        return row is null ? null : Map(row);
    }

    public async Task<Helper?> FindByPasswordAsync(string password)
    {
        var pw = (password ?? "").Trim();
        if (pw.Length == 0) return null;
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.FirstOrDefaultAsync<HelperRecord>(
            "WHERE Password = @0 AND Active = @1", pw, true);
        return row is null ? null : Map(row);
    }

    public async Task<Helper> AddAsync(int seasonId, string firstName, string lastName, string? email)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        string password = "";
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = HelperPassword.Generate();
            if (attempt >= 40) candidate += Random.Shared.Next(10, 100);
            var taken = await scope.Database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Helpers WHERE Password = @0", candidate);
            if (taken == 0) { password = candidate; break; }
        }
        if (password.Length == 0) password = HelperPassword.Generate() + Random.Shared.Next(1000, 10000);

        var helper = Helper.Create(seasonId, firstName, lastName, email, password);
        var row = new HelperRecord
        {
            SeasonId = helper.SeasonId,
            FirstName = helper.FirstName,
            LastName = helper.LastName,
            Email = helper.Email,
            Password = helper.Password,
            Active = helper.Active,
            CreatedAt = helper.CreatedAt
        };
        await scope.Database.InsertAsync(row);
        return Map(row);
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("UPDATE Helpers SET Active = @0 WHERE Id = @1", active, id);
    }

    public async Task DeleteAsync(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.DeleteAsync(new HelperRecord { Id = id });
    }

    private static Helper Map(HelperRecord r) =>
        Helper.FromPersistence(r.Id, r.SeasonId, r.FirstName ?? "", r.LastName ?? "", r.Email,
            r.Password, r.Active, r.CreatedAt);
}
