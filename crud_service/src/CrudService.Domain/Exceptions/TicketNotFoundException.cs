namespace CrudService.Domain.Exceptions;

public class TicketNotFoundException : Exception
{
    public TicketNotFoundException(long id)
        : base($"Ticket {id} no encontrado") { }
}
