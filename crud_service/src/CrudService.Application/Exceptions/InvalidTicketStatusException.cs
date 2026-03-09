namespace CrudService.Application.Exceptions;

public class InvalidTicketStatusException : Exception
{
    public InvalidTicketStatusException(string status)
        : base($"Estado de ticket inválido: {status}") { }
}
