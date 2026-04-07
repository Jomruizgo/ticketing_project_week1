# Quickstart — AuthService: HU1 + HU2

**Feature**: `001-user-auth` | **Branch**: `001-user-auth` | **Date**: 2026-04-07

---

## Prerequisitos

| Herramienta | Versión mínima |
|-------------|---------------|
| .NET SDK | 8.0 |
| Docker + Docker Compose | Docker 24+ |
| dotnet-ef | `dotnet tool install -g dotnet-ef` |
| PostgreSQL | 15 (via Docker) o local |

---

## 1. Clonar y moverse a la rama

```bash
git checkout 001-user-auth
cd authService
```

---

## 2. Configurar variables de entorno

Copia el archivo de ejemplo y ajusta los valores:

```bash
cp src/AuthService.Api/appsettings.Development.json.example src/AuthService.Api/appsettings.Development.json
```

Variables mínimas en `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "CAMBIAR_POR_SECRETO_SEGURO_32_CHARS_MIN",
    "ExpirationMinutes": 60,
    "Issuer": "AuthService",
    "Audience": "TicketingPlatform"
  },
  "LockoutSettings": {
    "MaxFailedAttempts": 3,
    "LockoutMinutes": 15
  }
}
```

> En producción, usa variables de entorno o `dotnet user-secrets`. Nunca hardcodees secretos.

---

## 3. Levantar PostgreSQL con Docker

```bash
docker compose up -d postgres
```

`docker-compose.yml` mínimo (en la raíz del repo):

```yaml
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: authdb
    ports:
      - "5432:5432"
```

---

## 4. Aplicar migraciones

```bash
dotnet ef database update \
  --project src/AuthService.Infrastructure \
  --startup-project src/AuthService.Api
```

---

## 5. Ejecutar la API

```bash
dotnet run --project src/AuthService.Api
```

La API estará disponible en `http://localhost:5000` (o el puerto configurado).

Swagger UI: `http://localhost:5000/swagger`

---

## 6. Probar manualmente (curl)

### Registro

```bash
curl -s -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Ana",
    "lastName": "Perez",
    "email": "ana.perez@example.com",
    "password": "Secr3t@Pass",
    "confirmPassword": "Secr3t@Pass"
  }' | jq
```

Respuesta esperada → `201 Created`:

```json
{
  "message": "Registro exitoso. Serás redirigido al login.",
  "redirect": "/login"
}
```

### Login

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ana.perez@example.com","password":"Secr3t@Pass"}' \
  | jq -r '.token')

echo "Token: $TOKEN"
```

Respuesta esperada → `200 OK`:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 3600
}
```

### Logout

```bash
curl -s -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer $TOKEN" | jq
```

Respuesta esperada → `200 OK`:

```json
{ "message": "Logout exitoso" }
```

---

## 7. Ejecutar tests

### Tests unitarios

```bash
dotnet test tests/AuthService.Application.Tests
```

### Tests de integración (requiere Docker)

```bash
dotnet test tests/AuthService.Infrastructure.Tests
```

### Todos los tests

```bash
dotnet test
```

---

## 8. Build Docker

```bash
docker build -t authservice:local .
docker run -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e JwtSettings__Secret="..." \
  authservice:local
```

---

## Estructura de proyectos (referencia rápida)

```
src/
├── AuthService.Domain/         ← Entidades + Puertos (sin dependencias externas)
├── AuthService.Application/    ← Casos de uso + DTOs
├── AuthService.Infrastructure/ ← EF Core + BCrypt + JWT
└── AuthService.Api/            ← Controllers + Composition Root

tests/
├── AuthService.Application.Tests/     ← Unitarios (xUnit + NSubstitute)
└── AuthService.Infrastructure.Tests/  ← Integración (Testcontainers)
```
