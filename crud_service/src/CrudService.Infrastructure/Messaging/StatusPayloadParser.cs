using System.Text.Json;

namespace CrudService.Infrastructure.Messaging;

/// <summary>
/// Contrato de evento compartido para mensajes ticket.status.changed.
/// Unifica la definición del payload entre consumer, tests y posibles futuros consumers.
/// </summary>
public record TicketStatusChangedPayload(long TicketId, string NewStatus, DateTime ChangedAt);

/// <summary>
/// Utilidades de parsing para payloads de eventos de estado de tickets.
/// Centraliza la deserialización defensiva y las opciones de JSON.
/// SRP: única responsabilidad — parsear payloads de mensajería.
/// </summary>
public static class StatusPayloadParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Intenta deserializar un JSON string a TicketStatusChangedPayload.
    /// Retorna null si el JSON es inválido, vacío o nulo.
    /// </summary>
    public static TicketStatusChangedPayload? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TicketStatusChangedPayload>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
