using System.Text.Json;
using CrudService.Application.Interfaces;

namespace CrudService.Infrastructure.Messaging;

/// <summary>
/// Aísla la creación de mensajes SSE para reducir fragilidad de tests
/// y centralizar el contrato de serialización hacia el frontend.
/// SRP: única responsabilidad — formatear TicketStatusUpdate como SSE data line.
/// </summary>
public static class SseMessageFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializa un TicketStatusUpdate al formato JSON del contrato SSE.
    /// Contrato: { "ticketId": N, "status": "..." }
    /// </summary>
    public static string ToSseJson(TicketStatusUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return JsonSerializer.Serialize(
            new { ticketId = update.TicketId, status = update.NewStatus },
            Options);
    }

    /// <summary>
    /// Formatea una línea SSE completa según la especificación Server-Sent Events.
    /// Formato: "data: {json}\n\n"
    /// </summary>
    public static string FormatSseLine(TicketStatusUpdate update)
    {
        return $"data: {ToSseJson(update)}\n\n";
    }
}
