using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using CrudService.Application.DTOs;
using NSubstitute;

namespace CrudService.Api.Tests;

/// <summary>
/// Pruebas de Caja Negra — Nivel: API HTTP.
///
/// Principio: el test NO conoce ni importa cómo está implementado internamente el servicio.
/// Solo interactúa a través de la interfaz pública HTTP (endpoints REST).
/// Esto verifica el CONTRATO de la API desde la perspectiva de un cliente externo.
///
/// Pirámide de testing: nivel más alto — verifica comportamiento observable completo.
/// 7 Principios — Principio 2 (Exhaustive testing is impossible): se prueban
/// las particiones equivalentes más representativas de cada endpoint.
/// </summary>
[Trait("Category", "BlackBox")]
public class TicketsApiBlackBoxTests : IClassFixture<CrudServiceApiFactory>
{
    private readonly HttpClient _client;
    private readonly CrudServiceApiFactory _factory;

    public TicketsApiBlackBoxTests(CrudServiceApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─── Health Check ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "GET /health → 200 OK con campo status=healthy")]
    public async Task GetHealth_ReturnsOkWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body);
    }

    [Fact(DisplayName = "GET /api/tickets/health → 200 OK (health del controlador)")]
    public async Task GetTicketsControllerHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/tickets/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── Validaciones de entrada (400 Bad Request) ───────────────────────────────

    [Fact(DisplayName = "POST /api/tickets/bulk con Quantity=0 → 400 Bad Request")]
    public async Task CreateTicketsBulk_WithZeroQuantity_ReturnsBadRequest()
    {
        var payload = new { EventId = 1, Quantity = 0 };

        var response = await _client.PostAsJsonAsync("/api/tickets/bulk", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/tickets/bulk con Quantity=9999 → 400 Bad Request")]
    public async Task CreateTicketsBulk_WithExcessiveQuantity_ReturnsBadRequest()
    {
        var payload = new { EventId = 1, Quantity = 9999 };

        var response = await _client.PostAsJsonAsync("/api/tickets/bulk", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/tickets/bulk con EventId=0 → 400 Bad Request")]
    public async Task CreateTicketsBulk_WithInvalidEventId_ReturnsBadRequest()
    {
        var payload = new { EventId = 0, Quantity = 10 };

        var response = await _client.PostAsJsonAsync("/api/tickets/bulk", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "PUT /api/tickets/{id}/status con NewStatus vacío → 400 Bad Request")]
    public async Task UpdateTicketStatus_WithEmptyStatus_ReturnsBadRequest()
    {
        var payload = new { NewStatus = "" };

        var response = await _client.PutAsJsonAsync("/api/tickets/1/status", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── Recursos no encontrados (404) ──────────────────────────────────────────

    [Fact(DisplayName = "GET /api/tickets/{id} cuando el ticket no existe → 404 Not Found")]
    public async Task GetTicket_WhenNotFound_Returns404()
    {
        _factory.TicketServiceMock
            .GetTicketByIdAsync(Arg.Any<long>())
            .Returns((TicketDto?)null);

        var response = await _client.GetAsync("/api/tickets/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── Respuestas correctas con datos mockeados ────────────────────────────────

    [Fact(DisplayName = "GET /api/tickets/{id} cuando existe → 200 OK con JSON del ticket")]
    public async Task GetTicket_WhenExists_ReturnsOkWithTicketJson()
    {
        var expectedTicket = new TicketDto
        {
            Id = 42,
            EventId = 1,
            Status = "available",
            Version = 0
        };

        _factory.TicketServiceMock
            .GetTicketByIdAsync(42)
            .Returns(expectedTicket);

        var response = await _client.GetAsync("/api/tickets/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("42", body);
        Assert.Contains("available", body);
    }

    [Fact(DisplayName = "GET /api/tickets/event/{eventId} → 200 OK con lista de tickets")]
    public async Task GetTicketsByEvent_ReturnsOkWithList()
    {
        var tickets = new List<TicketDto>
        {
            new() { Id = 1, EventId = 5, Status = "available" },
            new() { Id = 2, EventId = 5, Status = "reserved" }
        };

        _factory.TicketServiceMock
            .GetTicketsByEventAsync(5)
            .Returns(tickets);

        var response = await _client.GetAsync("/api/tickets/event/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("available", body);
        Assert.Contains("reserved", body);
    }
}
