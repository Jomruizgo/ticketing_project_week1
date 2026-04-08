# Quickstart: Notificación In-App SSE de Lista de Espera

**Feature**: 004-inapp-notification  
**Date**: 2026-04-07

---

## Prerequisitos

- Docker y Docker Compose instalados
- .NET 8 SDK
- El sistema completo levantado: `docker compose up -d --build`
- RabbitMQ configurado: `scripts/setup-rabbitmq.sh` ejecutado (automático vía `compose.yml`)
- Al menos una inscripción activa en lista de espera (HU1 implementada, spec 001)
- Asignación de oportunidades funcional (HU3 implementada, spec 003)

## Verificar el endpoint SSE

### 1. Conectarse al stream SSE

```bash
curl -N -H "Accept: text/event-stream" \
  "http://localhost:5062/api/waitlist/stream?email=comprador@ejemplo.com"
```

La conexión queda abierta. Se recibirán comentarios keep-alive cada 30 segundos:

```
: keepalive
```

### 2. Activar una oportunidad (desde otra terminal)

Publicar un evento `ticket.released` en RabbitMQ para disparar la asignación:

```bash
docker exec rabbitmq rabbitmqadmin publish \
  exchange=tickets routing_key=ticket.released \
  payload='{"ticketId":1,"eventId":42,"releasedAt":"2026-04-07T11:00:00Z"}'
```

### 3. Verificar el evento SSE recibido

En la terminal del paso 1, debería aparecer:

```
event: opportunity_activated
data: {"opportunityId":1,"ticketId":1,"eventId":42,"expiresAt":"2026-04-07T11:15:00Z","remainingMinutes":15}
```

### 4. Verificar validación de email

```bash
curl -v "http://localhost:5062/api/waitlist/stream?email=invalido"
# Esperado: 400 Bad Request
```

### 5. Verificar consulta posterior

```bash
curl "http://localhost:5062/api/waitlist/entries?eventId=42&email=comprador@ejemplo.com"
# Debería mostrar la oportunidad activa con remainingMinutes
```

## Ejecutar tests

```bash
# Tests unitarios del hub y del consumer
dotnet test crud_service/tests/CrudService.Infrastructure.Tests --filter "Sse" --no-restore
```

## Variables de entorno configurables

| Variable | Default | Descripción |
|----------|---------|-------------|
| `SSE_KEEPALIVE_INTERVAL_SECONDS` | 30 | Intervalo de keep-alive |
| `SSE_MAX_CONNECTIONS_PER_EMAIL` | 5 | Máx. conexiones por email |
