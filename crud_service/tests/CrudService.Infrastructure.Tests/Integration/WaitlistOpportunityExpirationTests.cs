using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Infrastructure.Persistence;
using CrudService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace CrudService.Infrastructure.Tests.Integration;

[Trait("Category", "Integration")]
public class WaitlistOpportunityExpirationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private TicketingDbContext _context = null!;
    private WaitlistOpportunityRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var schemaPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
            "scripts", "schema.sql");
        var schemaSql = await File.ReadAllTextAsync(schemaPath);

        await using (var bootstrapConn = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await bootstrapConn.OpenAsync();
            await using var schemaCmd = new NpgsqlCommand(schemaSql, bootstrapConn);
            await schemaCmd.ExecuteNonQueryAsync();
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
        dataSourceBuilder.MapEnum<WaitlistEntryStatus>("waitlist_entry_status");
        dataSourceBuilder.MapEnum<WaitlistOpportunityStatus>("waitlist_opportunity_status");
        dataSourceBuilder.MapEnum<NotificationDeliveryStatus>("notification_delivery_status");
        var dataSource = dataSourceBuilder.Build();

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql(dataSource)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new TicketingDbContext(options);
        _repository = new WaitlistOpportunityRepository(_context);

        await SeedBaseDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // === T031: active → expired with expired_at and expiration_reason persisted ===
    [Fact(DisplayName = "TC-HU6-INT-01: active opportunity persists expired_at and expiration_reason")]
    public async Task UpdateAsync_ActiveToExpired_PersistsExpiredAtAndExpirationReason()
    {
        var opportunity = await CreateActiveOpportunityInDbAsync();

        opportunity.TransitionTo(WaitlistOpportunityStatus.Expired);
        opportunity.ExpiredAt = DateTime.UtcNow;
        opportunity.ExpirationReason = "ttl_expired";

        await _repository.UpdateAsync(opportunity);

        // Re-read from DB to verify persistence
        var reloaded = await _context.WaitlistOpportunities
            .AsNoTracking()
            .FirstAsync(o => o.Id == opportunity.Id);

        Assert.Equal(WaitlistOpportunityStatus.Expired, reloaded.Status);
        Assert.NotNull(reloaded.ExpiredAt);
        Assert.Equal("ttl_expired", reloaded.ExpirationReason);
    }

    // === T032: FindByIdAsync with eager loading of WaitlistEntry ===
    [Fact(DisplayName = "TC-HU6-INT-02: FindByIdAsync loads WaitlistEntry eagerly")]
    public async Task FindByIdAsync_ExistingOpportunity_LoadsWaitlistEntryEagerly()
    {
        var opportunity = await CreateActiveOpportunityInDbAsync();

        // Detach to force reload
        _context.ChangeTracker.Clear();

        var loaded = await _repository.FindByIdAsync(opportunity.Id);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.WaitlistEntry);
        Assert.Equal("integration@test.com", loaded.WaitlistEntry.BuyerEmail);
    }

    [Fact(DisplayName = "TC-HU6-INT-03: FindByIdAsync returns null for non-existent id")]
    public async Task FindByIdAsync_NonExistentId_ReturnsNull()
    {
        var loaded = await _repository.FindByIdAsync(999999L);
        Assert.Null(loaded);
    }

    // === T031c: Full cycle — expiration → second opportunity also expires → columns persisted ===
    [Fact(DisplayName = "TC-HU6-INT-04: two sequential expirations persist independently")]
    public async Task FullCycle_TwoExpirations_BothPersistCorrectly()
    {
        // First opportunity expires
        var opp1 = await CreateActiveOpportunityInDbAsync();
        opp1.TransitionTo(WaitlistOpportunityStatus.Expired);
        opp1.ExpiredAt = DateTime.UtcNow;
        opp1.ExpirationReason = "ttl_expired";
        await _repository.UpdateAsync(opp1);

        // Second opportunity for same ticket (simulating reassignment then re-expiration)
        var entry2 = new WaitlistEntry
        {
            EventId = 1,
            BuyerEmail = "buyer2@test.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _context.WaitlistEntries.Add(entry2);
        await _context.SaveChangesAsync();

        var opp2 = new WaitlistOpportunity
        {
            WaitlistEntryId = entry2.Id,
            TicketId = 1,
            Status = WaitlistOpportunityStatus.Pending
        };
        _context.WaitlistOpportunities.Add(opp2);
        await _context.SaveChangesAsync();

        opp2.TransitionTo(WaitlistOpportunityStatus.Active);
        opp2.ActivatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        opp2.TransitionTo(WaitlistOpportunityStatus.Expired);
        opp2.ExpiredAt = DateTime.UtcNow;
        opp2.ExpirationReason = "ttl_expired";
        await _repository.UpdateAsync(opp2);

        // Verify both are independently expired
        _context.ChangeTracker.Clear();
        var reloaded1 = await _context.WaitlistOpportunities.AsNoTracking().FirstAsync(o => o.Id == opp1.Id);
        var reloaded2 = await _context.WaitlistOpportunities.AsNoTracking().FirstAsync(o => o.Id == opp2.Id);

        Assert.Equal(WaitlistOpportunityStatus.Expired, reloaded1.Status);
        Assert.Equal(WaitlistOpportunityStatus.Expired, reloaded2.Status);
        Assert.NotNull(reloaded1.ExpiredAt);
        Assert.NotNull(reloaded2.ExpiredAt);
        Assert.Equal("ttl_expired", reloaded1.ExpirationReason);
        Assert.Equal("ttl_expired", reloaded2.ExpirationReason);
    }

    private async Task SeedBaseDataAsync()
    {
        var @event = new Event { Name = "Integration Test Event", StartsAt = DateTime.UtcNow.AddDays(30) };
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();

        var ticket = new Ticket
        {
            EventId = @event.Id,
            Status = TicketStatus.Available
        };
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
    }

    private async Task<WaitlistOpportunity> CreateActiveOpportunityInDbAsync()
    {
        var entry = new WaitlistEntry
        {
            EventId = 1,
            BuyerEmail = "integration@test.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-30)
        };
        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        var opportunity = new WaitlistOpportunity
        {
            WaitlistEntryId = entry.Id,
            TicketId = 1,
            Status = WaitlistOpportunityStatus.Pending
        };
        _context.WaitlistOpportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        opportunity.TransitionTo(WaitlistOpportunityStatus.Active);
        opportunity.ActivatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return opportunity;
    }
}
