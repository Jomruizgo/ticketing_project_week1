# Quickstart: Inscripción en Lista de Espera

**Feature**: 001-waitlist-enrollment  
**Date**: 2026-04-07

## Pre-requisitos

- Docker y Docker Compose instalados
- .NET 8 SDK instalado
- El repositorio clonado y en la rama `001-waitlist-enrollment`

## 1. Levantar infraestructura

```bash
docker compose up -d rabbitmq postgres
```

## 2. Aplicar schema de BD

El script `scripts/schema.sql` debe incluir la tabla `waitlist_entries` y su partial unique index. Si es la primera vez:

```bash
docker compose exec postgres psql -U postgres -d ticketing -f /docker-entrypoint-initdb.d/schema.sql
```

## 3. Ejecutar pruebas unitarias (TDD RED → GREEN)

```bash
cd crud_service
dotnet test tests/CrudService.Application.Tests --filter "Waitlist"
```

Estos tests cubren:
- TC-HU1-01: Inscripción exitosa
- TC-HU1-02: Rechazo de duplicado
- TC-HU1-03: Rechazo por lista cerrada
- TC-HU1-04: Reinscripción válida

## 4. Ejecutar pruebas de integración

```bash
dotnet test tests/CrudService.Infrastructure.Tests --filter "Waitlist"
```

Requiere Docker corriendo (Testcontainers levanta PostgreSQL automáticamente).

Cubre:
- TC-HU1-05: Unicidad a nivel de base de datos

## 5. Ejecutar el CRUD Service localmente

```bash
dotnet run --project src/CrudService.Api/CrudService.Api.csproj
```

## 6. Probar el endpoint

```bash
# Inscripción exitosa
curl -X POST http://localhost:5062/api/waitlist/entries \
  -H "Content-Type: application/json" \
  -d '{"eventId": 1, "buyerEmail": "test@ejemplo.com"}'
# Esperado: 201 Created

# Duplicado
curl -X POST http://localhost:5062/api/waitlist/entries \
  -H "Content-Type: application/json" \
  -d '{"eventId": 1, "buyerEmail": "test@ejemplo.com"}'
# Esperado: 409 Conflict
```

## 7. Ejecutar todo con Docker Compose

```bash
docker compose up -d --build
```

El CRUD Service estará disponible en el puerto configurado en `compose.yml`.
