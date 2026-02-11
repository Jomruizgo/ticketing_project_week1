# ReservationService - Consumer Service 1

Microservicio responsable de procesar las reservas de tickets consumiendo mensajes de RabbitMQ.

## Responsabilidad

- Consumir mensajes de la cola `q.ticket.reserved`
- Validar disponibilidad del ticket
- Actualizar estado del ticket a `reserved` en PostgreSQL
- Registrar `reserved_at` y `expires_at` para control de tiempo
- Rechazar reservas si el ticket ya no está disponible

## Stack

- .NET 8 (LTS)
- RabbitMQ.Client
- Entity Framework Core + Npgsql (PostgreSQL)

## Estructura del Proyecto

```
ReservationService/
├── ReservationService.sln
├── Dockerfile
├── README.md
├── src/
│   └── ReservationService.Worker/
│       ├── Consumers/        # Consumers de RabbitMQ
│       ├── Models/           # DTOs y entidades
│       ├── Services/         # Lógica de negocio
│       ├── Repositories/     # Interfaces de acceso a datos
│       ├── Data/             # DbContext y configuraciones EF
│       ├── Configurations/   # Clases de configuración
│       ├── Extensions/       # Métodos de extensión
│       ├── Program.cs
│       └── appsettings.json
├── tests/
└── docs/
```

## Configuracion

### Variables de Entorno

| Variable | Descripcion | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL | - |
| `RabbitMQ__Host` | Host de RabbitMQ | localhost |
| `RabbitMQ__Port` | Puerto AMQP | 5672 |
| `RabbitMQ__Username` | Usuario RabbitMQ | guest |
| `RabbitMQ__Password` | Password RabbitMQ | guest |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ticketing_db;Username=ticketing_user;Password=ticketing_password"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "QueueName": "q.ticket.reserved"
  }
}
```

## Ejecucion Local

```bash
# Desde la raiz del microservicio
cd ReservationService

# Restaurar dependencias
dotnet restore

# Ejecutar
dotnet run --project src/ReservationService.Worker
```

## Ejecucion con Docker

```bash
# Build
docker build -t reservation-service .

# Run
docker run --env-file .env reservation-service
```

## Flujo de Procesamiento

```
[RabbitMQ: q.ticket.reserved]
         │
         ▼
┌─────────────────────────┐
│   TicketConsumer        │
│   (BackgroundService)   │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│   ReservationService    │
│   - Validar ticket      │
│   - Verificar estado    │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│   TicketRepository      │
│   - UPDATE tickets      │
│   - Control version     │
└───────────┬─────────────┘
            │
            ▼
     [PostgreSQL]
```

## Criterios de Aceptacion

- [ ] Si el ticket ya no esta DISPONIBLE, el mensaje debe descartarse
- [ ] Usar optimistic locking (campo `version`) para evitar race conditions
- [ ] Registrar `reserved_at` y calcular `expires_at` (+5 minutos)
- [ ] Loguear errores cuando no se pueda procesar una reserva

## Lo que la IA hizo mal

> Esta seccion se llena conforme se identifican malas practicas sugeridas por la IA

| Fecha | Descripcion | Correccion |
|-------|-------------|------------|
| | | |

---

**Responsable:** Jorge
