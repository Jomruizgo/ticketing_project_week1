using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Contrato para el acceso a datos del historial de cambios de estado de tickets.
/// Permite registrar y consultar la auditoría de transiciones de estado.
/// </summary>
public interface ITicketHistoryRepository
{
    /// <summary>
    /// Registra un nuevo cambio de estado en el historial del ticket.
    /// </summary>
    /// <param name="history">Registro de historial a almacenar.</param>
    Task AddAsync(TicketHistory history);

    /// <summary>
    /// Obtiene todo el historial de cambios de estado de un ticket, ordenado cronológicamente.
    /// </summary>
    /// <param name="ticketId">Identificador del ticket.</param>
    /// <returns>Lista de registros de historial ordenados por fecha de cambio.</returns>
    Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId);
}