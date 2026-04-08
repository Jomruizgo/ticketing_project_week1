# Quickstart: Expiración de Oportunidad de Lista de Espera

**Feature**: 006-opportunity-expiration  
**Date**: 2026-04-08

## Prerrequisitos

- .NET SDK 8.0
- Docker (para Testcontainers y Docker Compose)
- PostgreSQL y RabbitMQ corriendo (via `docker compose up -d`)

## Compilar

```bash
cd crud_service
dotnet build CrudService.sln
```

## Ejecutar tests

### Tests unitarios (handler)

```bash
dotnet test tests/CrudService.Application.Tests/CrudService.Application.Tests.csproj \
  --filter "FullyQualifiedName~ExpireOpportunity"
```

### Tests de integración (Testcontainers)

```bash
dotnet test tests/CrudService.Infrastructure.Tests/CrudService.Infrastructure.Tests.csproj \
  --filter "Category=Integration&FullyQualifiedName~Expiration"
```

### Todos los tests

```bash
dotnet test CrudService.sln
```

## Verificación manual con Docker Compose

1. Levantar infraestructura:
```bash
docker compose up -d --build
```

2. Verificar que el consumer está escuchando (logs):
```bash
docker compose logs -f crud-service 2>&1 | grep -i "expired\|expiration"
```

3. Crear un escenario de expiración:
   - Crear un evento con entradas (via API existente)
   - Agotar todas las entradas (reservar/pagar)
   - Inscribir un comprador en la lista de espera (POST /api/waitlist/entries)
   - Liberar una entrada (trigger ticket.released)
   - Esperar 15 minutos (o ajustar `WAITLIST_OPPORTUNITY_TTL_MS` a un valor corto como 30000 para testing)
   - Verificar que la oportunidad cambió a `expired` (GET /api/waitlist/entries?eventId=X&email=Y)

4. Verificar reasignación: inscribir dos compradores antes de liberar la entrada. Al expirar la primera oportunidad, verificar que la segunda se asigna automáticamente.

5. Verificar retorno al inventario: inscribir un solo comprador. Al expirar su oportunidad, verificar que la entrada vuelve al inventario (availableTickets incrementa).

## Variables de entorno relevantes

| Variable | Default | Descripción |
|---|---|---|
| `WAITLIST_OPPORTUNITY_TTL_MS` | `900000` (15 min) | Período de validez de la oportunidad |
| `RABBITMQ_HOST` | `rabbitmq` | Host de RabbitMQ |
| `RABBITMQ_PORT` | `5672` | Puerto de RabbitMQ |
| `RABBITMQ_USER` | `guest` | Usuario de RabbitMQ |
| `RABBITMQ_PASS` | `guest` | Password de RabbitMQ |
