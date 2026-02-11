using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Contrato para el acceso a datos de pagos.
/// Define operaciones CRUD sobre la tabla de pagos.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Obtiene el pago asociado a un ticket específico.
    /// </summary>
    /// <param name="ticketId">Identificador del ticket.</param>
    /// <returns>El pago encontrado o null si no existe.</returns>
    Task<Payment?> GetByTicketIdAsync(long ticketId);

    /// <summary>
    /// Obtiene un pago por su identificador, incluyendo el ticket asociado.
    /// </summary>
    /// <param name="id">Identificador del pago.</param>
    /// <returns>El pago encontrado o null si no existe.</returns>
    Task<Payment?> GetByIdAsync(long id);

    /// <summary>
    /// Actualiza los datos de un pago existente.
    /// </summary>
    /// <param name="payment">Entidad pago con los datos actualizados.</param>
    /// <returns>True si la actualización fue exitosa; false en caso contrario.</returns>
    Task<bool> UpdateAsync(Payment payment);

    /// <summary>
    /// Crea un nuevo registro de pago en la base de datos.
    /// Asigna automáticamente las fechas de creación y actualización.
    /// </summary>
    /// <param name="payment">Entidad pago a crear.</param>
    /// <returns>El pago creado con su identificador asignado.</returns>
    Task<Payment> CreateAsync(Payment payment);
}