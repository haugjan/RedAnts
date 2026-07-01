using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing;

public sealed class SingleTicketRepository(IScopeProvider scopeProvider) : ISingleTickets
{
    public async Task<IReadOnlyList<SingleTicket>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<SingleTicketRecord>(new Sql("SELECT * FROM SingleTickets ORDER BY PurchasedAt DESC"));
        scope.Complete();
        return records.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SingleTicket>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<SingleTicketRecord>(
            new Sql("SELECT * FROM SingleTickets WHERE EventId = @0 ORDER BY PurchasedAt DESC", eventId));
        scope.Complete();
        return records.Select(Map).ToList();
    }

    public async Task<SingleTicket?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<SingleTicketRecord>(new Sql("SELECT * FROM SingleTickets WHERE Id = @0", id));
        scope.Complete();
        return record is null ? null : Map(record);
    }

    public async Task<SingleTicket?> FindByTicketGuidAsync(Guid ticketId)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<SingleTicketRecord>(
            new Sql("SELECT * FROM SingleTickets WHERE TicketId = @0", ticketId.ToString()));
        scope.Complete();
        return record is null ? null : Map(record);
    }

    public async Task<SingleTicket> SaveAsync(SingleTicket ticket)
    {
        using var scope = scopeProvider.CreateScope();
        var record = ToRecord(ticket);
        await scope.Database.InsertAsync(record);
        scope.Complete();
        return Map(record);
    }

    public async Task UpdateAsync(SingleTicket ticket)
    {
        using var scope = scopeProvider.CreateScope();
        var record = ToRecord(ticket);
        record.Id = ticket.Id;
        await scope.Database.UpdateAsync(record);
        scope.Complete();
    }

    private static SingleTicket Map(SingleTicketRecord r) =>
        SingleTicket.FromPersistence(
            r.Id, r.EventId, TicketingMappers.ParseEnum(r.PriceCategory, PriceCategory.Adult), r.Price,
            DateTime.SpecifyKind(r.PurchasedAt, DateTimeKind.Utc),
            r.UsedAt is null ? null : DateTime.SpecifyKind(r.UsedAt.Value, DateTimeKind.Utc),
            r.CheckedInAt is null ? null : DateTime.SpecifyKind(r.CheckedInAt.Value, DateTimeKind.Utc),
            Guid.TryParse(r.TicketId, out var g) ? g : Guid.Empty,
            TicketingMappers.ReadBilling(r.BillingFirstName, r.BillingLastName, r.BillingStreet, r.BillingAddressLine2,
                r.BillingPostalCode, r.BillingCity, r.BillingCountry, r.BillingEmail, r.BillingPhone),
            TicketingMappers.ParseEnum(r.PaymentMethod, PaymentMethod.Payrexx),
            TicketingMappers.ParseEnum(r.PayStatus, PayStatus.Pending));

    private static SingleTicketRecord ToRecord(SingleTicket t)
    {
        var b = t.BillingAddress;
        return new SingleTicketRecord
        {
            EventId = t.EventId,
            PriceCategory = t.PriceCategory.ToString(),
            Price = t.Price,
            PurchasedAt = t.PurchasedAt,
            UsedAt = t.UsedAt,
            CheckedInAt = t.CheckedInAt,
            TicketId = t.TicketId.ToString(),
            BillingFirstName = b.FirstName,
            BillingLastName = b.LastName,
            BillingStreet = b.Street,
            BillingAddressLine2 = b.AddressLine2,
            BillingPostalCode = b.PostalCode.Value,
            BillingCity = b.City,
            BillingCountry = b.Country,
            BillingEmail = b.Email,
            BillingPhone = b.Phone,
            PaymentMethod = t.PaymentMethod.ToString(),
            PayStatus = t.PayStatus.ToString()
        };
    }
}

public sealed class SeasonTicketRepository(IScopeProvider scopeProvider) : ISeasonTickets
{
    public async Task<IReadOnlyList<SeasonTicket>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<SeasonTicketRecord>(new Sql("SELECT * FROM SeasonTickets ORDER BY PurchasedAt DESC"));
        scope.Complete();
        return records.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SeasonTicket>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<SeasonTicketRecord>(
            new Sql("SELECT * FROM SeasonTickets WHERE SeasonId = @0 ORDER BY PurchasedAt DESC", seasonId));
        scope.Complete();
        return records.Select(Map).ToList();
    }

    public async Task<SeasonTicket?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<SeasonTicketRecord>(new Sql("SELECT * FROM SeasonTickets WHERE Id = @0", id));
        scope.Complete();
        return record is null ? null : Map(record);
    }

    public async Task<SeasonTicket?> FindByTicketGuidAsync(Guid seasonTicketId)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<SeasonTicketRecord>(
            new Sql("SELECT * FROM SeasonTickets WHERE SeasonTicketId = @0", seasonTicketId.ToString()));
        scope.Complete();
        return record is null ? null : Map(record);
    }

    public async Task<SeasonTicket> SaveAsync(SeasonTicket ticket)
    {
        using var scope = scopeProvider.CreateScope();
        var record = ToRecord(ticket);
        await scope.Database.InsertAsync(record);
        scope.Complete();
        return Map(record);
    }

    public async Task UpdateAsync(SeasonTicket ticket)
    {
        using var scope = scopeProvider.CreateScope();
        var record = ToRecord(ticket);
        record.Id = ticket.Id;
        await scope.Database.UpdateAsync(record);
        scope.Complete();
    }

    private static SeasonTicket Map(SeasonTicketRecord r) =>
        SeasonTicket.FromPersistence(
            r.Id, r.SeasonId, TicketingMappers.ParseEnum(r.Category, SeasonTicketCategory.Extern),
            TicketingMappers.ParseEnum(r.AgeGroup, AgeGroup.Adult), r.Price,
            DateTime.SpecifyKind(r.PurchasedAt, DateTimeKind.Utc),
            Guid.TryParse(r.SeasonTicketId, out var g) ? g : Guid.Empty,
            TicketingMappers.ReadBilling(r.BillingFirstName, r.BillingLastName, r.BillingStreet, r.BillingAddressLine2,
                r.BillingPostalCode, r.BillingCity, r.BillingCountry, r.BillingEmail, r.BillingPhone),
            TicketingMappers.ParseEnum(r.PaymentMethod, PaymentMethod.Payrexx),
            TicketingMappers.ParseEnum(r.PayStatus, PayStatus.Pending),
            r.RemainingAdmissions,
            r.CheckedInAt is null ? null : DateTime.SpecifyKind(r.CheckedInAt.Value, DateTimeKind.Utc));

    private static SeasonTicketRecord ToRecord(SeasonTicket t)
    {
        var b = t.BillingAddress;
        return new SeasonTicketRecord
        {
            SeasonId = t.SeasonId,
            Category = t.Category.ToString(),
            AgeGroup = t.AgeGroup.ToString(),
            Price = t.Price,
            PurchasedAt = t.PurchasedAt,
            SeasonTicketId = t.SeasonTicketId.ToString(),
            BillingFirstName = b.FirstName,
            BillingLastName = b.LastName,
            BillingStreet = b.Street,
            BillingAddressLine2 = b.AddressLine2,
            BillingPostalCode = b.PostalCode.Value,
            BillingCity = b.City,
            BillingCountry = b.Country,
            BillingEmail = b.Email,
            BillingPhone = b.Phone,
            PaymentMethod = t.PaymentMethod.ToString(),
            PayStatus = t.PayStatus.ToString(),
            RemainingAdmissions = t.RemainingAdmissions,
            CheckedInAt = t.CheckedInAt
        };
    }
}

public sealed class MemberCardRepository(IScopeProvider scopeProvider) : IMemberCards
{
    public async Task<IReadOnlyList<MemberCard>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<MemberCardRecord>(new Sql("SELECT * FROM MemberCards ORDER BY LastName, FirstName"));
        scope.Complete();
        return records.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<MemberCard>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<MemberCardRecord>(
            new Sql("SELECT * FROM MemberCards WHERE SeasonId = @0 ORDER BY LastName, FirstName", seasonId));
        scope.Complete();
        return records.Select(Map).ToList();
    }

    public async Task<MemberCard?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<MemberCardRecord>(new Sql("SELECT * FROM MemberCards WHERE Id = @0", id));
        scope.Complete();
        return record is null ? null : Map(record);
    }

    public async Task<MemberCard> SaveAsync(MemberCard card)
    {
        using var scope = scopeProvider.CreateScope();
        var record = ToRecord(card);
        await scope.Database.InsertAsync(record);
        scope.Complete();
        return Map(record);
    }

    public async Task UpdateAsync(MemberCard card)
    {
        using var scope = scopeProvider.CreateScope();
        var record = ToRecord(card);
        record.Id = card.Id;
        await scope.Database.UpdateAsync(record);
        scope.Complete();
    }

    public async Task DeleteAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(new Sql("DELETE FROM MemberCards WHERE Id = @0", id));
        scope.Complete();
    }

    private static MemberCard Map(MemberCardRecord r) =>
        MemberCard.FromPersistence(r.Id, r.FirstName, r.LastName, r.Birthday.ToDateOnly(), r.SeasonId);

    private static MemberCardRecord ToRecord(MemberCard c) => new()
    {
        FirstName = c.FirstName,
        LastName = c.LastName,
        Birthday = c.Birthday.ToDateTime(),
        SeasonId = c.SeasonId
    };
}

public sealed class TicketScanLogRepository(IScopeProvider scopeProvider) : ITicketScanLog
{
    public async Task AppendAsync(TicketScanEntry entry)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.InsertAsync(new TicketScanLogRecord
        {
            TicketKind = entry.Kind.ToString(),
            TicketRef = entry.TicketRef.ToString(),
            Direction = entry.Direction.ToString(),
            ScannedAt = entry.ScannedAt,
            ScannedBy = entry.ScannedBy
        });
        scope.Complete();
    }

    public async Task<IReadOnlyList<TicketScanEntry>> GetForTicketAsync(Guid ticketRef)
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<TicketScanLogRecord>(
            new Sql("SELECT * FROM TicketScanLog WHERE TicketRef = @0 ORDER BY ScannedAt", ticketRef.ToString()));
        scope.Complete();
        return records.Select(r => new TicketScanEntry(
            TicketingMappers.ParseEnum(r.TicketKind, TicketKind.Single),
            Guid.TryParse(r.TicketRef, out var g) ? g : Guid.Empty,
            TicketingMappers.ParseEnum(r.Direction, ScanDirection.In),
            DateTime.SpecifyKind(r.ScannedAt, DateTimeKind.Utc), r.ScannedBy)).ToList();
    }
}
