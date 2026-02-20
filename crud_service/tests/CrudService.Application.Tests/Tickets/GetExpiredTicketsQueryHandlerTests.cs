using CrudService.Application.UseCases.Tickets.GetExpiredTickets;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Tickets;

public class GetExpiredTicketsQueryHandlerTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly GetExpiredTicketsQueryHandler _sut;

    public GetExpiredTicketsQueryHandlerTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _sut = new GetExpiredTicketsQueryHandler(_ticketRepo);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMappedDtos()
    {
        // Arrange
        var expiredTickets = new List<Ticket>
        {
            new() { Id = 1, EventId = 10, Status = TicketStatus.Reserved, ExpiresAt = DateTime.UtcNow.AddMinutes(-5),  Version = 1 },
            new() { Id = 2, EventId = 10, Status = TicketStatus.Reserved, ExpiresAt = DateTime.UtcNow.AddMinutes(-10), Version = 1 }
        };
        _ticketRepo.GetExpiredAsync(Arg.Any<DateTime>()).Returns(expiredTickets);

        // Act
        var result = (await _sut.HandleAsync(new GetExpiredTicketsQuery())).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("Reserved", dto.Status));
    }
}
