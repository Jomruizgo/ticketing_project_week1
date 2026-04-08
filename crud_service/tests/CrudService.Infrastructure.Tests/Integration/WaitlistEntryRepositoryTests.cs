using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Exceptions;
using CrudService.Infrastructure.Persistence;
using CrudService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CrudService.Infrastructure.Tests.Integration;

[Trait("Category", "Integration")]
public class WaitlistEntryRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private TicketingDbContext _context = null!;
    private WaitlistEntryRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // 1. Run schema FIRST so enum types exist in the database
        var schemaPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "scripts", "schema.sql");
        var schemaSql = await File.ReadAllTextAsync(schemaPath);

        await using (var bootstrapConn = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await bootstrapConn.OpenAsync();
            await using var schemaCmd = new NpgsqlCommand(schemaSql, bootstrapConn);
            await schemaCmd.ExecuteNonQueryAsync();
        }

        // 2. Build DataSource AFTER enums exist so type catalog is loaded correctly
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
        dataSourceBuilder.MapEnum<WaitlistEntryStatus>("waitlist_entry_status");
        var dataSource = dataSourceBuilder.Build();

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql(dataSource)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new TicketingDbContext(options);
        _repository = new WaitlistEntryRepository(_context);

        var @event = new Event { Name = "Test Event", StartsAt = DateTime.UtcNow.AddDays(30) };
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact(DisplayName = "TC-HU1-05: Partial unique index rejects duplicate active entry")]
    public async Task AddAsync_DuplicateActiveEntry_ThrowsDuplicateException()
    {
        var firstEntry = new WaitlistEntry
        {
            EventId = 1,
            BuyerEmail = "unique@test.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };
        await _repository.AddAsync(firstEntry);

        var duplicateEntry = new WaitlistEntry
        {
            EventId = 1,
            BuyerEmail = "unique@test.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<DuplicateWaitlistEntryException>(
            () => _repository.AddAsync(duplicateEntry));
    }

    [Fact(DisplayName = "TC-HU1-05 complement: Re-enrollment after consumed succeeds at DB level")]
    public async Task AddAsync_DuplicateAfterConsumed_Succeeds()
    {
        var firstEntry = new WaitlistEntry
        {
            EventId = 1,
            BuyerEmail = "reenroll@test.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };
        await _repository.AddAsync(firstEntry);

        firstEntry.Status = WaitlistEntryStatus.Consumed;
        await _context.SaveChangesAsync();

        var newEntry = new WaitlistEntry
        {
            EventId = 1,
            BuyerEmail = "reenroll@test.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };
        var result = await _repository.AddAsync(newEntry);

        Assert.NotNull(result);
        Assert.Equal(WaitlistEntryStatus.Active, result.Status);
        Assert.True(result.Id > firstEntry.Id);
    }
}
