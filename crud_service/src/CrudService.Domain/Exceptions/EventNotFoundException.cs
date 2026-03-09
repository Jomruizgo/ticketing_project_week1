namespace CrudService.Domain.Exceptions;

public class EventNotFoundException : Exception
{
    public EventNotFoundException(long id)
        : base($"Evento {id} no encontrado") { }
}
