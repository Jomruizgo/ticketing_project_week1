# Quickstart: Asignación de Oportunidad de Lista de Espera

**Feature**: 003-waitlist-opportunity-assignment  
**Date**: 2026-04-07

---

## Prerequisitos

1. PostgreSQL corriendo con el esquema actualizado (`scripts/schema.sql`)
2. RabbitMQ corriendo con la topología declarada (`scripts/setup-rabbitmq.sh`)
3. .NET 8 SDK instalado

## Build

```bash
cd crud_service
dotnet build src/CrudService.Api/CrudService.Api.csproj
```

## Tests unitarios

```bash
cd crud_service
dotnet test tests/CrudService.Application.Tests --filter "AssignOpportunity"
```

## Tests de integración

```bash
cd crud_service
dotnet test tests/CrudService.Infrastructure.Tests --filter "FifoStrategy"
```

## Todos los tests de Waitlist

```bash
cd crud_service
dotnet test tests/CrudService.Application.Tests --filter "Waitlist"
```

## Ejecución local

```bash
# Infraestructura completa
docker compose up -d --build

# Solo el CRUD Service en local
dotnet run --project crud_service/src/CrudService.Api/CrudService.Api.csproj
```

## Verificación manual

Publicar un mensaje `ticket.released` en RabbitMQ:

```bash
rabbitmqadmin publish exchange=tickets routing_key=ticket.released \
  payload='{"ticketId":1,"eventId":1,"releasedAt":"2026-04-07T10:00:00Z"}'
```

Verificar en los logs del CRUD Service que el consumer procesa el mensaje y, si hay inscripciones activas, crea una oportunidad.

## Variables de entorno relevantes

| Variable | Default | Descripción |
|----------|---------|-------------|
| `WAITLIST_OPPORTUNITY_TTL_MS` | `900000` | Vigencia de la oportunidad en ms (15 min) |
| `ConnectionStrings__DefaultConnection` | — | Connection string PostgreSQL |
| `RabbitMQ__Host` | `localhost` | Host de RabbitMQ |
| `RabbitMQ__Port` | `5672` | Puerto de RabbitMQ |
