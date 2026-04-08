# Quickstart — 005-email-notification

**Date**: 2026-04-08

## Prerrequisitos

- .NET 8 SDK instalado
- Docker y Docker Compose (para PostgreSQL y RabbitMQ)
- El proyecto `crud_service` compila sin errores

## Levantar infraestructura

```bash
# Desde la raíz del repo
docker compose up -d postgres rabbitmq rabbitmq-setup
```

## Aplicar esquema de BD

```bash
# Si la tabla notification_deliveries no existe aún
docker compose exec postgres psql -U postgres -d ticketing -f /docker-entrypoint-initdb.d/schema.sql
```

O ejecutar manualmente el DDL de `data-model.md` contra la BD.

## Ejecutar tests

```bash
# Tests unitarios (incluye refactor de HU3 + nuevos de HU5)
cd crud_service
dotnet test tests/CrudService.Application.Tests
dotnet test tests/CrudService.Infrastructure.Tests
```

## Ejecutar el servicio localmente

```bash
dotnet run --project crud_service/src/CrudService.Api
```

## Verificar la feature

1. Inscribir un comprador en lista de espera (POST `/api/waitlist/enroll`)
2. Liberar un ticket para disparar la asignación (el `TicketReleasedConsumer` invoca `AssignOpportunityHandler`)
3. Verificar en logs que `EmailNotificationObserver` se ejecutó y `LogEmailSender` registró el intento
4. Consultar la tabla `notification_deliveries` para verificar el registro de auditoría:
   ```sql
   SELECT * FROM notification_deliveries 
   WHERE waitlist_opportunity_id = <id> 
   ORDER BY sent_at DESC;
   ```

## Variables de entorno relevantes

| Variable | Default | Descripción |
|----------|---------|-------------|
| `EMAIL_SENDER_FROM` | — | Dirección de remitente del correo |
| `EMAIL_SENDER_TIMEOUT_MS` | `5000` | Timeout de comunicación con el proveedor |
| `WAITLIST_OPPORTUNITY_TTL_MS` | `900000` | TTL de la oportunidad (15 min). Ya existe de HU3 |

## Notas

- El MVP usa `LogEmailSender` (solo loguea, no envía correo real). Para conectar un proveedor real, cambiar el registro DI de `IEmailSender` en `DependencyInjection.cs`.
- La feature no expone endpoints nuevos. El observer se ejecuta automáticamente cuando se activa una oportunidad.
