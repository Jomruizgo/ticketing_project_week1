# Ticketing Project - Week 1

Sistema distribuido de gestión de tickets y eventos usando arquitectura de microservicios con RabbitMQ.

## 📋 Visión General

Aplicación que demuestra patrones de arquitectura distribuida:
- **Async Communication** con eventos y colas
- **Event-Driven Architecture** usando RabbitMQ
- **Microservices Pattern** con servicios independientes
- **Resilience Patterns** con reintentos y recuperación automática

## 🗺️ Diagrama de Arquitectura

```
┌────────────────────────────────────────────────────────────────────┐
│                           CLIENTE                                  │
│                   Frontend  Next.js  :3000                         │
└──────────────┬──────────────────────────────┬─────── ──────────────┘
               │ HTTP (sync)                  │ SSE (EventSource)
               │ POST /reserve                │ ◄── text/event-stream
               │ POST /payments (202 Accepted)│     (notificación única)
               ▼                              ▼
  ┌───────────────────────┐     ┌─────────────────────────────────┐
  │   Producer  :8001     │     │         CRUD Service  :8002     │
  │  ─ TicketsController  │     │  ─ EventsController             │
  │  ─ PaymentsController │     │  ─ TicketsController            │
  │  (solo publica,       │     │  ─ GET /tickets/{id}/stream     ◄──┐
  │   no decide pagos)    │     │  ─ TicketStatusConsumer (worker)│  │
  └───────────┬───────────┘     └────────────────────────────┬────┘  │
              │                              │ SQL queries   │ SSE   │
              │ Publish (async)              ▼               │ hub   │
              │                    ┌─────────────────┐       │       │
              │                    │   PostgreSQL    │       │       │
              │                    │     :5432       │       │       │
              │                    │  tickets        │       │       │
              │                    │  events         │       │       │
              │                    │  payments       │       │       │
              │                    │  ticket_history │       │       │
              │                    └────────┬────────┘       │       │
              ▼                             │ UPDATE         │       │
  ┌───────────────────────────────────────────────────────┐  │       │
  │               RabbitMQ  (exchange: tickets, topic)    │  │       │
  │                                                       │  │       │
  │  ticket.reserved          ──► q.ticket.reserved       │  │       │
  │  ticket.payment.requested ──► q.ticket.payment.req.   │  │       │
  │  ticket.payments.approved ──► q.ticket.payments.appr. │  │       │
  │  ticket.payments.rejected ──► q.ticket.payments.rej.  │  │       │
  │  ticket.status.changed    ──► q.ticket.status.changed ├──┘       │
  └──────────┬───────────────────────────┬────────────────┘          │
             │                           │                           │
             ▼                           ▼                           │
  ┌────────────────────┐     ┌───────────────── ──────┐              │
  │ ReservationService │     │   PaymentService       │              │
  │     (Worker)       │     │     (Worker)           │              │
  │  ─ Hexagonal arch  │     │  ─ Consume requested   │              │
  │  ─ ProcessReserv.  │     │  ─ Decide ✓/✗ pago     │              │
  │  ─ Update DB       │     │  ─ Consume appr./rej.  │              │
  │  ─ Publica         │     │  ─ Update DB           │              │
  │    status.changed  │     │  ─ Publica             │              │
  └────────┬───────────┘     │    status.changed      │              │
           │                 └────────────┬───────────┘              │
           │  ticket.status.changed       │                          │
           └──────────────┬──────── ──────┘                          │
                          │                                          │
                          └─────────────► RabbitMQ ─► CrudService  ──┘
```

### Colas y routing keys

| Routing key                    | Cola                             | Consumer                         |
|-------------------------------|----------------------------------|----------------------------------|
| `ticket.reserved`             | `q.ticket.reserved`              | ReservationService               |
| `ticket.payment.requested`    | `q.ticket.payment.requested`     | PaymentService                   |
| `ticket.payments.approved`    | `q.ticket.payments.approved`     | PaymentService                   |
| `ticket.payments.rejected`    | `q.ticket.payments.rejected`     | PaymentService                   |
| `ticket.status.changed`       | `q.ticket.status.changed`        | CrudService (notifica SSE)       |

### Flujo resumido

| Acción                | Ruta                                                                                     |
|----------------------|------------------------------------------------------------------------------------------|
| Ver eventos          | Frontend → CRUD Service → PostgreSQL                                                     |
| Reservar ticket      | Frontend → Producer → RabbitMQ → ReservationService → PostgreSQL → `status.changed` → SSE |
| Procesar pago        | Frontend → Producer → RabbitMQ → PaymentService → PostgreSQL → `status.changed` → SSE   |
| Notificación estado  | CrudService consume `ticket.status.changed` → SSE push → Frontend (EventSource)         |

## 🎯 Servicios

### 1. CRUD Service (Puerto 8002)
- **Responsabilidad**: Persistencia de datos + notificaciones SSE
- **Database**: PostgreSQL 15
- **Endpoints**:
  - `GET /api/events` - Listar eventos
  - `POST /api/events` - Crear evento
  - `GET /api/tickets/{eventId}` - Listar tickets
  - `POST /api/tickets` - Crear tickets
  - `PATCH /api/tickets/{id}` - Actualizar ticket
  - `GET /api/tickets/{id}/stream` - Stream SSE del estado del ticket
  - `GET /health` - Health check
- **BackgroundService**: `TicketStatusConsumer` consume `q.ticket.status.changed` y notifica a los clientes SSE conectados vía `TicketStatusHub` (in-memory)

### 2. Producer Service (Puerto 8001)
- **Responsabilidad**: Publicación de eventos a RabbitMQ (solo publica, no decide)
- **Message Broker**: RabbitMQ 3.12
- **Endpoints**:
  - `POST /api/tickets/reserve` - Publica `ticket.reserved` (→ 202 Accepted)
  - `POST /api/payments/process` - Publica `ticket.payment.requested` (→ 202 Accepted)
  - `GET /health` - Health check

### 3. Frontend (Puerto 3000)
- **Framework**: Next.js 14
- **Pages**:
  - `/buy` - Compra de tickets (Buyer view)
  - `/buy/[id]` - Detalle de evento y compra

## 📦 Flujos de Datos

### Flujo 1: Reserva de Ticket
```
Frontend
  ├─ Crea evento (CRUD Service)
  ├─ Crea tickets (CRUD Service)
  └─ Reserva ticket
     │
     ├─► Producer Service: POST /api/tickets/reserve (202 Accepted)
     │       └─ Publica: ticket.reserved
     │               └─► RabbitMQ ──► ReservationService
     │                                   ├─ Actualiza DB: status = "reserved"
     │                                   └─ Publica: ticket.status.changed
     │                                               └─► RabbitMQ ──► CrudService
     │                                                                   └─ SSE push
     └─► Frontend abre EventSource GET /api/tickets/{id}/stream
             └─ Recibe evento SSE con status = "reserved"
```

### Flujo 2: Pago de Ticket
```
Frontend (después de reserva)
  │
  ├─► Producer Service: POST /api/payments/process (202 Accepted)
  │       └─ Publica: ticket.payment.requested
  │               └─► RabbitMQ ──► PaymentService (decide)
  │                                   │
  │                                   ├─ 80% éxito → Publica ticket.payments.approved
  │                                   │               └─► RabbitMQ ──► PaymentService
  │                                   │                                   ├─ INSERT payment (DB)
  │                                   │                                   ├─ UPDATE ticket: status = "paid"
  │                                   │                                   └─ Publica ticket.status.changed
  │                                   │
  │                                   └─ 20% fallo → Publica ticket.payments.rejected
  │                                                   └─► RabbitMQ ──► PaymentService
  │                                                                       ├─ UPDATE ticket: status = "released"
  │                                                                       └─ Publica ticket.status.changed
  │
  │      ticket.status.changed ──► CrudService consumer ──► SSE push
  │
  └─► Frontend abre EventSource GET /api/tickets/{id}/stream
          └─ Recibe evento SSE con status = "paid" | "released"
```

## 🚀 Inicio Rápido

### Requisitos
- Docker & Docker Compose
- .NET 8.0 SDK
- Node.js 18+ (Frontend)
- Git

### Pasos

1. **Clonar y navegar**
```bash
git clone <repo>
cd ticketing_project_week0
```

2. **Iniciar servicios con Docker**
```bash
docker-compose up -d --build
```

3. **Iniciar Frontend**
```bash
cd frontend
npm install
npm run dev
```

4. **Acceder**
- Frontend: http://localhost:3000
- CRUD API: http://localhost:8002/swagger
- Producer API: http://localhost:8001/swagger
- RabbitMQ UI: http://localhost:15672 (guest:guest)

## 📚 Documentación

### Producer Service
- [PAYMENTS.md](./producer/PAYMENTS.md) - Endpoints de pagos
- [PAYMENT_SYSTEM.md](./producer/PAYMENT_SYSTEM.md) - Arquitectura completa
- [ARCHITECTURE.md](./producer/ARCHITECTURE.md) - Diseño general

### CRUD Service
- [PAYMENT_CONSUMER.md](./crud_service/PAYMENT_CONSUMER.md) - Cómo implementar consumer de pagos

### General
- [PAYMENT_IMPLEMENTATION_SUMMARY.md](./PAYMENT_IMPLEMENTATION_SUMMARY.md) - Resumen de lo implementado

## 🧪 Testing

### Con curl/Postman

**1. Crear Evento**
```bash
curl -X POST http://localhost:8002/api/events \
  -H "Content-Type: application/json" \
  -d '{"name":"Concierto Rock","startsAt":"2026-02-20T20:00:00Z"}'
```

**2. Crear Tickets**
```bash
curl -X POST http://localhost:8002/api/tickets \
  -H "Content-Type: application/json" \
  -d '{"eventId":1,"quantity":10}'
```

**3. Reservar Ticket**
```bash
curl -X POST http://localhost:8001/api/tickets/reserve \
  -H "Content-Type: application/json" \
  -d '{
    "eventId":1,
    "ticketId":1,
    "orderId":"ORD-001",
    "reservedBy":"user@email.com",
    "expiresInSeconds":600
  }'
```

**4. Procesar Pago**
```bash
curl -X POST http://localhost:8001/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId":1,
    "eventId":1,
    "amountCents":5000,
    "currency":"USD",
    "paymentBy":"user@email.com",
    "paymentMethodId":"card_1234"
  }'
# Respuesta: 202 Accepted - el resultado llega vía SSE
```

**5. Escuchar estado por SSE**
```bash
curl -N http://localhost:8002/api/tickets/1/stream
# Espera hasta recibir: data: {"ticketId":1,"status":"paid"}
```

### Ver Logs
```bash
# CRUD Service
docker-compose logs -f crud-service

# Producer Service
docker-compose logs -f producer

# RabbitMQ
docker-compose logs -f rabbitmq
```

## 🔄 Patrones de Arquitectura Distribuida

| Patrón | Implementación | Ubicación |
|--------|---|---|
| **Event-Driven** | RabbitMQ + Topic Exchange | `tickets` exchange |
| **Async/Await** | 202 Accepted responses | Producer endpoints |
| **Circuit Breaker** | Health checks | `/health` endpoints |
| **Message Persistence** | Durable queues | RabbitMQ config |
| **SSE (push)** | EventSource / `text/event-stream` | CrudService + Frontend |
| **Microservices** | CRUD + Producer + Workers | Separate ports |
| **Idempotency** | TransactionRef | Payment events |
| **In-process pub/sub** | `Channel<T>` (bounded) | `TicketStatusHub` |

## 📊 RabbitMQ Topics

| Topic | Routing Key | Publicado por | Consumido por |
|-------|---|---|---|
| `tickets` | `ticket.reserved` | Producer | ReservationService |
| `tickets` | `ticket.payment.requested` | Producer | PaymentService |
| `tickets` | `ticket.payments.approved` | PaymentService | PaymentService |
| `tickets` | `ticket.payments.rejected` | PaymentService | PaymentService |
| `tickets` | `ticket.status.changed` | ReservationService / PaymentService | CrudService |

## 🎓 Conceptos Demostrados

### 1. Comunicación Asincrónica
- Requests devuelven 202 Accepted inmediatamente
- Procesamiento ocurre en background
- El Producer no decide resultados de negocio: solo publica eventos

### 2. Event Sourcing
- Cada acción genera un evento
- Eventos se almacenan en RabbitMQ
- Multiple consumers pueden reaccionar

### 3. Desacoplamiento
- Servicios no conocen otros servicios
- Comunicación solo a través de eventos
- Fácil agregar nuevos consumers

### 4. Resiliencia
- Si CRUD Service cae, eventos persisten en RabbitMQ
- Si Producer cae, Frontend recibe error pero puede reintentar
- Transacciones garantizan consistencia

## 🔧 Stack Técnico

### Backend
- **.NET 8.0** - Framework
- **Entity Framework Core** - ORM
- **PostgreSQL 15** - Base de datos
- **RabbitMQ 3.12** - Message broker
- **RabbitMQ.Client** - Driver
- **Swagger/OpenAPI** - Documentación

### Frontend
- **Next.js 14** - Framework
- **React 18** - UI
- **TypeScript** - Type safety
- **Tailwind CSS** - Estilos
- **SWR** - Data fetching
- **Sonner** - Notificaciones

### Infrastructure
- **Docker & Docker Compose** - Containerización
- **PostgreSQL 15** - Persistence
- **RabbitMQ 3.12** - Messaging

## 🤝 Estructura del Proyecto

```
ticketing_project_week1/
├── ReservationService/              # Worker: procesa reservas (arquitectura hexagonal)
│   ├── src/
│   │   ├── ReservationService.Domain/        # Entidades puras + interfaces (puertos)
│   │   ├── ReservationService.Application/   # Casos de uso (sin dependencias externas)
│   │   ├── ReservationService.Infrastructure/# EF Core + RabbitMQ (adaptadores)
│   │   └── ReservationService.Worker/        # Composition root (solo Program.cs)
│   └── tests/
│       └── ReservationService.Application.Tests/
├── paymentService/                  # Worker: procesa pagos (estructura plana - pendiente migrar)
│   └── MsPaymentService.Worker/
│       ├── Handlers/
│       ├── Messaging/
│       ├── Repositories/
│       └── Services/
├── crud_service/                    # API REST: gestión de eventos y tickets
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   └── Data/
├── producer/                        # API REST: publica eventos a RabbitMQ
│   ├── src/
│   │   ├── Producer.Domain/
│   │   ├── Producer.Application/
│   │   ├── Producer.Infrastructure/
│   │   └── Producer.Api/
│   └── tests/
│       └── Producer.Application.Tests/
├── frontend/                        # Next.js: interfaz de usuario
│   ├── app/
│   │   ├── buy/                     # Vista comprador
│   │   └── admin/                   # Vista admin (pendiente)
│   ├── components/
│   ├── hooks/
│   └── lib/
├── scripts/                         # SQL, setup RabbitMQ, datos de prueba
├── compose.yml                      # Docker Compose
└── README.md
```

## � Lo Que la IA Hizo Mal

Como parte de nuestro enfoque **AI-First**, documentamos decisiones donde rechazamos sugerencias de la IA por ser anti-patrones:

### Rechazo 1: Credenciales Hardcodeadas en Código
**Situación:** La IA sugirió crear la conexión RabbitMQ con credenciales directas:
```csharp
var factory = new ConnectionFactory 
{ 
    HostName = "rabbitmq.prod.com", 
    Password = "admin123"  // ❌ CRÍTICO
};
```
**Por qué rechazamos:** Nunca exponer secrets en repositorio. Usamos `IOptions<RabbitMQOptions>` inyectadas por DI, cargadas desde `appsettings.json` + variables de entorno. ✅ Ahora las credenciales están seguras en `.env` (ignorado en Git).
Sin embargo, la IA alucina demasiado cuando se trata de mucas referencias a secretos.

### Rechazo 2: CORS AllowAll en Producción
**Situación:** La IA generó:
```csharp
policy.AllowAnyOrigin()  // Permite requests de cualquier dominio
      .AllowAnyMethod()
      .AllowAnyHeader();
```
**Por qué rechazamos:** Vulnerabilidad CSRF y exposición a ataques cross-origin. Aunque lo mantuvimos para desarrollo, está documentado que debe restringirse a `http://localhost:3000` en producción o a su dominio respectivo y usar credenciales.

### Rechazo 3: No considerar la liberación del ticket cuando el usuario no paga
**Situación:** La IA no diseñó un mecanismo claro para liberar tickets cuando el usuario no completa el pago (o cuando el pago expira). En algunos borradores la IA asumió que los tickets se liberarían manualmente o por monitorización externa.
**Por qué rechazamos:** Esto deja tickets reservados indefinidamente en escenarios de fallo, generando bloqueo de inventario. Se decidió implementar un job/worker que libere reservas expiradas o que el consumer que confirma la reserva fije `expires_at` y garantice la liberación automática cuando corresponda.

### Rechazo 4: Producer intentó reservar y fijar `expiresAt`
**Situación:** La IA propuso que el `Producer` reservara el ticket y fijara la fecha de caducidad (`expiresAt`) antes de que el `Consumer` confirmara la reserva en la base de datos.
**Por qué rechazamos:** La expiración debe fijarse en el momento en que la reserva es persistida (consumer) para evitar problemas de latencia y condiciones de carrera. Si el `Producer` calcula `expiresAt` y falla la entrega o el consumer tarda en procesar, la ventana de expiración puede quedar desalineada (expiraciones que empiezan antes de la reserva real). Por eso la lógica de reserva y del `expires_at` se implementó en el `ReservationService` (consumer) con `// HUMAN CHECK` explicando la decisión.

### Rechazo 5: Uso de `docker compose` vs `docker-compose` y versión forzada en `compose`
**Situación:** En propuestas iniciales la IA generó archivos y ejemplos usando `docker compose.yml` o forzando la versión `3.8` del esquema de compose.

**Por qué rechazamos:** Las prácticas actuales recomiendan usar el archivo `compose.yml` (o `docker-compose.yml` según convención del proyecto) y no imponer una versión antigua de formato sin necesidad. Forzar `3.8` puede ser innecesario o incompatible con algunos entornos; además, la referencia a `docker compose.yml` es confusa (se usa `docker compose` sin guión en la CLI moderna). Se documentó que el repositorio adopta `compose.yml` y la sintaxis moderna, y que cualquier sugerencia de la IA sobre nombres/versions debe validarse antes de aplicarla.

### Rechazo 6: Confusión de la IA entre Inyección de Dependencias y uso directo de `.env`
**Situación:** En varias propuestas la IA generó cambios que ignoraban la inyección de dependencias (`IOptions<T>` en .NET) y en su lugar recomendó embebecer valores o leer `.env` directamente dentro del código de producción.
**Por qué rechazamos:** Esto rompe la abstracción de DI, dificulta pruebas unitarias y copia secretos en lugares no gestionados. En este repo mantenemos la convención: registrar opciones/configuraciones por DI y poblarlas desde `appsettings.json` + variables de entorno o un secret manager. Cualquier cambio propuesto por la IA que modifique el flujo de configuración debe revisarse manualmente (`// HUMAN CHECK`) antes de integrarlo.

### Rechazo 7: Acoplar adapters de entrada a handlers concretos
**Situación:** La IA dejó adapters de entrada consumiendo clases concretas de casos de uso (por ejemplo, controllers de `Producer` y el consumer de `ReservationService`) en lugar de depender de puertos de entrada.
**Por qué rechazamos:** En hexagonal estricto, la capa de infraestructura/API debe depender de contratos (interfaces), no de implementaciones concretas. Se corrigió introduciendo puertos de entrada (`IProcessReservationUseCase`, `IReserveTicketUseCase`, `IRequestPaymentUseCase`) y actualizando DI para resolver interfaz -> handler. En la misma revisión se eliminaron abstracciones huérfanas (`IMessageConsumer`) y código muerto (`TicketNotAvailableException`).

---

## �📝 Notas Importantes

1. **Simulación de Pagos**: Los pagos tienen 80% probabilidad de éxito simulada. En producción se integraría con Stripe/PayPal.

2. **Frontend**: Solo implementada la vista del buyer para el mvp. Admin view pendiente.

3. **CRUD Consumer**: El CRUD Service necesita implementar el consumer de pagos.

4. **SSE**: Frontend abre una conexión `EventSource` a `GET /api/tickets/{id}/stream`. CrudService cierra el stream en cuanto publica el primer evento de estado (máx 30s de timeout). No hay polling.

## 🚨 Troubleshooting

**CORS Error?**
- Producer Service tiene CORS habilitado en Program.cs
- Si sigue fallando, revisar puerto del frontend (3000)

**RabbitMQ no conecta?**
- Verificar que RabbitMQ esté up: `docker-compose ps`
- Revisar logs: `docker-compose logs rabbitmq`
- Reset: `docker-compose down -v && docker-compose up -d`

**Tickets no se actualizan?**
- Verificar CRUD Service logs
- Revisar que consumer de eventos esté activo
- Revisar bindings en RabbitMQ UI


## 🛡 Instancias `// HUMAN CHECK` en el código

Registramos varias validaciones manuales (`// HUMAN CHECK`) en el código donde el equipo revisó y corrigió decisiones sugeridas por la IA. Estas ubicaciones sirven como evidencia y guía para nuevos desarrolladores:

- `ReservationService` (optimistic locking) — [ReservationService/src/ReservationService.Worker/Services/ReservationService.cs](ReservationService/src/ReservationService.Worker/Services/ReservationService.cs#L17)
- `TicketRepository` (optimistic locking, reserva) — [ReservationService/src/ReservationService.Worker/Repositories/TicketRepository.cs](ReservationService/src/ReservationService.Worker/Repositories/TicketRepository.cs#L23)
- `CrudService` DI / DbContext scope — [crud_service/Extensions/ServiceExtensions.cs](crud_service/Extensions/ServiceExtensions.cs#L21)
- `RabbitMQPaymentPublisher` (mensajes persistentes) — [producer/src/Producer.Infrastructure/Messaging/RabbitMQPaymentPublisher.cs](producer/src/Producer.Infrastructure/Messaging/RabbitMQPaymentPublisher.cs#L30)
- `Producer` CORS (policy para desarrollo vs producción) — [producer/src/Producer.Api/Program.cs](producer/src/Producer.Api/Program.cs#L19)
- `Producer` RabbitMQ config (nota sobre secrets) — [producer/src/Producer.Infrastructure/Messaging/RabbitMQSettings.cs](producer/src/Producer.Infrastructure/Messaging/RabbitMQSettings.cs#L1)

Por favor revise esas ubicaciones al integrarse al proyecto; cada `// HUMAN CHECK` explica la decisión del equipo y el riesgo que se mitigó.
