namespace CrudService.Application.UseCases.Events.UpdateEvent;

public record UpdateEventCommand(long Id, string? Name, DateTime? StartsAt);
