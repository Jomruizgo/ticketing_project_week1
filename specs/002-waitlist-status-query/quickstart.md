# Quickstart: Consulta de Estado de Lista de Espera

**Feature**: 002-waitlist-status-query  
**Date**: 2026-04-07

## Prerequisitos

- .NET 8 SDK instalado
- PostgreSQL con schema aplicado (`scripts/schema.sql`)
- Inscripción de espera previa creada (feature 001)

## Ejecución local

```bash
# Desde la raíz del repositorio

# 1. Levantar infraestructura
docker compose up -d postgres rabbitmq

# 2. Ejecutar CrudService
dotnet run --project crud_service/src/CrudService.Api/CrudService.Api.csproj
```

## Probar el endpoint

### Consultar estado de inscripción

```bash
# Inscripción existente (200)
curl -s "http://localhost:5104/api/waitlist/entries?eventId=42&email=comprador@ejemplo.com" | jq .

# Inscripción no encontrada (404)
curl -s -w "\n%{http_code}" "http://localhost:5104/api/waitlist/entries?eventId=42&email=nadie@ejemplo.com"

# Parámetros faltantes (400)
curl -s -w "\n%{http_code}" "http://localhost:5104/api/waitlist/entries?eventId=42"

# Email inválido (400)
curl -s -w "\n%{http_code}" "http://localhost:5104/api/waitlist/entries?eventId=42&email=invalido"
```

## Ejecutar tests

```bash
# Tests unitarios del handler
dotnet test crud_service/tests/CrudService.Application.Tests/CrudService.Application.Tests.csproj \
  --filter "GetWaitlistStatus"

# Todos los tests de waitlist
dotnet test crud_service/tests/CrudService.Application.Tests/CrudService.Application.Tests.csproj \
  --filter "Waitlist"
```

## Flujo TDD esperado

1. **RED**: Escribir tests para `GetWaitlistStatusHandler` (5 casos: inscripción activa sin oportunidad, oportunidad activa con tiempo restante, oportunidad consumida, oportunidad expirada, inscripción no encontrada).
2. **GREEN**: Implementar `GetWaitlistStatusQuery`, `GetWaitlistStatusHandler`, DTOs, extensión de repositorio y acción GET del controller.
3. **REFACTOR**: Extraer lógica de cálculo de minutes remaining si se repite.
