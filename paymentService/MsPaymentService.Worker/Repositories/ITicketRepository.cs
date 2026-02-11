using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Contrato para el acceso a datos de tickets.
/// Define operaciones de lectura y escritura sobre la tabla de tickets.
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// Obtiene un ticket por su identificador, incluyendo los pagos asociados.
    /// </summary>
    /// <param name="id">Identificador del ticket.</param>
    /// <returns>El ticket encontrado o null si no existe.</returns>
    Task<Ticket?> GetByIdAsync(long id);

    /// <summary>
    /// Obtiene un ticket con bloqueo pesimista (SELECT FOR UPDATE) para evitar
    /// condiciones de carrera en transiciones de estado concurrentes.
    /// </summary>
    /// <param name="id">Identificador del ticket.</param>
    /// <returns>El ticket bloqueado o null si no existe.</returns>
    Task<Ticket?> GetByIdForUpdateAsync(long id);

    /// <summary>
    /// Actualiza el estado y campos de un ticket usando concurrencia optimista.
    /// Solo actualiza si la versión del registro coincide.
    /// </summary>
    /// <param name="ticket">Entidad ticket con los datos actualizados.</param>
    /// <returns>True si la actualización fue exitosa; false en caso contrario.</returns>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">Si el ticket fue modificado por otro proceso.</exception>
    Task<bool> UpdateAsync(Ticket ticket);

    /// <summary>
    /// Obtiene todos los tickets reservados cuya reserva ha expirado.
    /// </summary>
    /// <param name="expirationThreshold">Fecha límite de expiración (UTC).</param>
    /// <returns>Lista de tickets con reservas expiradas.</returns>
    Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold);
}