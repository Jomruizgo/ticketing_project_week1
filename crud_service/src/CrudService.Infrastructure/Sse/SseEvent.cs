namespace CrudService.Infrastructure.Sse;

public record SseEvent(string EventType, string Data);
