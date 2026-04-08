# TicketRush MVP

Sistema distribuido de venta de entradas con lista de espera, construido con arquitectura de microservicios, mensajería asíncrona (RabbitMQ) y notificaciones en tiempo real (SSE).

## 📋 Visión General

Plataforma de ticketing que implementa:
- **Arquitectura Hexagonal (DDD)** en todos los servicios backend
- **Event-Driven Architecture** con RabbitMQ (topic exchange)
- **Consistencia eventual** con patrón 202 Accepted + SSE push
- **Sistema de lista de espera** completo: inscripción → asignación de oportunidad → expiración automática (DLX) → notificación por email/SSE → claim
- **Observer pattern (GoF)** para extensibilidad de notificaciones

## 🗺️ Diagrama de Arquitectura

```
┌────────────────────────────────────────────────────────────────────────┐
│                            CLIENTE                                     │
│              Frontend  Next.js (App Router)  :3000                     │
│  ─ /events/[id]  (compra + waitlist enroll + claim opportunity)        │
│  ─ /buy          (flujo de compra)                                     │
│  ─ /admin        (gestión de eventos)                                  │
└──────────────┬──────────────────────────────────┬──────────────────────┘
               │ HTTP (sync)                      │ SSE (EventSource)
               │ POST /reserve                    │ ◄── text/event-stream
               │ POST /payments (202 Accepted)    │     tickets + waitlist
               ▼                                  ▼
  ┌───────────────────────┐     ┌──────────────────────────────────────┐
  │   Producer  :8001     │     │          CRUD Service  :8002         │
  │  ─ TicketsController  │     │  ─ EventsController                  │
  │  ─ PaymentsController │     │  ─ TicketsController (+ SSE stream)  │
  │  (solo publica,       │     │  ─ WaitlistController                │
  │   no decide pagos)    │     │     POST /entries (enroll)           │
  └───────────┬───────────┘     │     GET  /entries (status query)     │
              │                 │     GET  /stream  (SSE waitlist)     │
              │ Publish         │     POST /opportunities/{id}/claim   │
              │ (async)         │  ─ Hexagonal: Domain → App → Infra  │
              │                 └────────────────────────────────┬─────┘
              │                              │ SQL              │ SSE
              │                              ▼                  │ hub
              │                    ┌─────────────────┐          │
              │                    │   PostgreSQL     │          │
              │                    │     :5432        │          │
              │                    │  events          │          │
              │                    │  tickets         │          │
              │                    │  payments        │          │
              │                    │  ticket_history  │          │
              │                    │  waitlist_entries │          │
              │                    │  waitlist_opps   │          │
              │                    │  notif_deliveries│          │
              │                    └────────┬────────┘          │
              ▼                             │                   │
  ┌─────────────────────────────────────────┴───────────────────┴───┐
  │               RabbitMQ  (exchange: tickets, topic)              │
  │                                                                 │
  │  ticket.reserved            ──► q.ticket.reserved               │
  │  ticket.payment.requested   ──► q.ticket.payment.requested      │
  │  ticket.payments.approved   ──► q.ticket.payments.approved      │
  │  ticket.payments.rejected   ──► q.ticket.payments.rejected      │
  │  ticket.status.changed      ──► q.ticket.status.changed         │
  │  ticket.released            ──► q.ticket.released               │
  │  ticket.returned_to_inventory ► q.ticket.returned               │
  │  waitlist.opportunity.expired► q.waitlist.opportunity.expired    │
  │  (DLX) q.waitlist.opportunity.delay → q.waitlist.opp.expired    │
  │  (DLX) q.ticket.reserved.delay     → q.ticket.expired          │
  └──────┬──────────────────────────────┬───────────────────────────┘
         │                              │
         ▼                              ▼
  ┌────────────────────┐     ┌────────────────────────┐
  │ ReservationService │     │   PaymentService       │
  │     (Worker)       │     │     (Worker)           │
  │  ─ Hexagonal arch  │     │  ─ Hexagonal arch      │
  │  ─ ProcessReserv.  │     │  ─ Consume requested   │
  │  ─ Optimistic lock │     │  ─ Decide ✓/✗ pago     │
  │  ─ Publica         │     │  ─ Idempotencia        │
  │    status.changed  │     │  ─ Publica             │
  └────────────────────┘     │    status.changed      │
                             └────────────────────────┘
```

### Colas y routing keys

| Routing key                      | Cola                              | Consumer                              |
|----------------------------------|-----------------------------------|---------------------------------------|
| `ticket.reserved`                | `q.ticket.reserved`               | ReservationService                    |
| `ticket.payment.requested`       | `q.ticket.payment.requested`      | PaymentService                        |
| `ticket.payments.approved`       | `q.ticket.payments.approved`      | PaymentService                        |
| `ticket.payments.rejected`       | `q.ticket.payments.rejected`      | PaymentService                        |
| `ticket.status.changed`          | `q.ticket.status.changed`         | CrudService (SSE push)                |
| `ticket.released`                | `q.ticket.released`               | CrudService (inventario)              |
| `ticket.returned_to_inventory`   | `q.ticket.returned`               | CrudService (inventario)              |
| `waitlist.opportunity.expired`   | `q.waitlist.opportunity.expired`  | CrudService (ExpireOpportunityHandler)|
| — (DLX)                         | `q.waitlist.opportunity.delay`    | Dead-letter → expired (TTL 900s)      |
| — (DLX)                         | `q.ticket.reserved.delay`         | Dead-letter → expired (TTL 5min)      |

### Flujo resumido

| Acción                        | Ruta                                                                                         |
|-------------------------------|----------------------------------------------------------------------------------------------|
| Ver eventos                   | Frontend → CRUD Service → PostgreSQL                                                         |
| Reservar ticket               | Frontend → Producer → RabbitMQ → ReservationService → PostgreSQL → `status.changed` → SSE    |
| Procesar pago                 | Frontend → Producer → RabbitMQ → PaymentService → PostgreSQL → `status.changed` → SSE       |
| Notificación estado ticket    | CrudService consume `ticket.status.changed` → SSE push → Frontend (EventSource)             |
| Inscripción waitlist          | Frontend → CRUD Service `POST /waitlist/entries` → PostgreSQL                                |
| Asignar oportunidad           | CrudService `AssignOpportunityHandler` → publish DLX delay → Observer (SSE + Email)          |
| Expirar oportunidad           | DLX TTL expira → `q.waitlist.opportunity.expired` → `ExpireOpportunityHandler`               |
| Reclamar oportunidad          | Frontend → CRUD Service `POST /waitlist/opportunities/{id}/claim`                            |

## 🎯 Servicios

### 1. CRUD Service (Puerto 8002)
- **Responsabilidad**: Persistencia de datos + casos de uso de negocio + notificaciones SSE
- **Arquitectura**: Hexagonal (Domain → Application → Infrastructure → Api)
- **Database**: PostgreSQL 15
- **Endpoints**:
  - `GET /api/events` — Listar eventos
  - `POST /api/events` — Crear evento
  - `GET /api/events/{id}` — Obtener evento por ID
  - `PUT /api/events/{id}` — Actualizar evento
  - `DELETE /api/events/{id}` — Eliminar evento
  - `GET /api/tickets/event/{eventId}` — Listar tickets por evento
  - `GET /api/tickets/{id}` — Obtener ticket por ID
  - `POST /api/tickets/bulk` — Crear tickets
  - `PUT /api/tickets/{id}/status` — Actualizar estado de ticket
  - `GET /api/tickets/{id}/stream` — Stream SSE del estado del ticket
  - `POST /api/waitlist/entries` — Inscribirse en lista de espera
  - `GET /api/waitlist/entries` — Consultar estado de inscripción (`eventId` + `email`)
  - `GET /api/waitlist/stream` — SSE de notificaciones waitlist
  - `POST /api/waitlist/opportunities/{id}/claim` — Reclamar oportunidad
  - `GET /health` — Health check
- **BackgroundServices**:
  - `TicketStatusConsumer` — consume `q.ticket.status.changed` → SSE push
  - `SseNotificationConsumer` — consume oportunidades activadas → SSE push waitlist
  - `ExpireOpportunityConsumer` — consume `q.waitlist.opportunity.expired` → marca oportunidad expirada
- **Casos de uso (Application layer)**:
  - `EnrollInWaitlist` — inscripción con validación de duplicado y evento futuro
  - `GetWaitlistStatus` — consulta de estado con oportunidad activa
  - `AssignOpportunity` — asigna entrada liberada al siguiente inscrito, notifica vía Observer
  - `ExpireOpportunity` — expira oportunidad vencida, libera ticket al inventario
  - `ClaimOpportunity` — reclama oportunidad activa, reserva ticket
- **Observers (IOpportunityObserver)**:
  - `OpportunityActivatedObserver` — publica a RabbitMQ para SSE
  - `EmailNotificationObserver` — registra auditoría + envía email (stub MVP)

### 2. Producer Service (Puerto 8001)
- **Responsabilidad**: Publicación de eventos a RabbitMQ (solo publica, no decide)
- **Arquitectura**: Hexagonal (Domain → Application → Infrastructure → Api)
- **Message Broker**: RabbitMQ 3.12
- **Endpoints**:
  - `POST /api/tickets/reserve` — Publica `ticket.reserved` (→ 202 Accepted)
  - `POST /api/payments/process` — Publica `ticket.payment.requested` (→ 202 Accepted)
  - `GET /health` — Health check

### 3. Frontend (Puerto 3000)
- **Framework**: Next.js (App Router), React, TypeScript, Tailwind CSS, shadcn/ui
- **Páginas**:
  - `/events/[id]` — Detalle de evento: compra de tickets, inscripción en waitlist, claim de oportunidad
  - `/buy` — Catálogo de eventos (vista comprador)
  - `/admin` — Gestión de eventos (pendiente)
- **Componentes clave**:
  - `waitlist-enroll-form.tsx` — Formulario de inscripción a lista de espera
  - `waitlist-status.tsx` — Estado de oportunidad con claim y countdown
  - `reserve-ticket-dialog.tsx` — Diálogo de reserva
  - `payment-form.tsx` / `payment-status.tsx` — Flujo de pago
- **Hooks**:
  - `use-waitlist-sse.ts` — SSE para notificaciones de waitlist en tiempo real
  - `use-ticket-status-sse.ts` — SSE para estado de ticket
  - `use-reservation-status.ts` / `use-payment-status.ts` — Polling de estados
- **API client**: `lib/api.ts` — funciones tipadas para todos los endpoints

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

### Flujo 3: Lista de Espera (Waitlist)
```
1. INSCRIPCIÓN
   Frontend ──► CRUD Service: POST /api/waitlist/entries
                    ├─ Valida: evento existe, fecha futura, no duplicado
                    └─ Persiste waitlist_entry (status = "active")
                         └─ Respuesta: 201 Created | 409 Conflict | 422 Closed

2. ASIGNACIÓN (cuando se libera un ticket)
   ticket.released ──► CrudService AssignOpportunityHandler
       ├─ Busca siguiente inscrito activo (FIFO)
       ├─ Crea waitlist_opportunity (status = "active")
       ├─ Publica DLX delay (TTL configurable, default 15min)
       └─ Observer pattern (IEnumerable<IOpportunityObserver>):
            ├─ OpportunityActivatedObserver → RabbitMQ → SSE push
            └─ EmailNotificationObserver → log/email + auditoría BD

3. EXPIRACIÓN AUTOMÁTICA
   DLX TTL expira → q.waitlist.opportunity.expired
       → ExpireOpportunityHandler
            ├─ Marca oportunidad como "expired"
            └─ Libera ticket al inventario (status = "available")

4. CLAIM (reclamar oportunidad)
   Frontend ──► CRUD Service: POST /api/waitlist/opportunities/{id}/claim
       ├─ Valida: oportunidad activa, no expirada, email coincide
       ├─ Marca oportunidad como "consumed"
       └─ Reserva ticket para el comprador
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

**6. Inscribirse en lista de espera**
```bash
curl -X POST http://localhost:8002/api/waitlist/entries \
  -H "Content-Type: application/json" \
  -d '{"eventId":1,"buyerEmail":"user@email.com"}'
# 201 Created | 409 Conflict (duplicado) | 422 (cerrada)
```

**7. Consultar estado de inscripción**
```bash
curl "http://localhost:8002/api/waitlist/entries?eventId=1&email=user@email.com"
```

**8. Reclamar oportunidad**
```bash
curl -X POST http://localhost:8002/api/waitlist/opportunities/1/claim \
  -H "Content-Type: application/json" \
  -d '{"buyerEmail":"user@email.com"}'
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

## 🔄 Patrones de Arquitectura

| Patrón | Implementación | Ubicación |
|--------|---|---|
| **Hexagonal / Ports & Adapters** | Domain → Application → Infrastructure | Todos los servicios backend |
| **Event-Driven** | RabbitMQ + Topic Exchange | `tickets` exchange |
| **Async/Await** | 202 Accepted responses | Producer endpoints |
| **Observer (GoF)** | `IEnumerable<IOpportunityObserver>` | CrudService waitlist |
| **DLX (Dead Letter Exchange)** | TTL-based expiration | Oportunidades + reservas |
| **Message Persistence** | Durable queues | RabbitMQ config |
| **SSE (push)** | EventSource / `text/event-stream` | CrudService → Frontend |
| **Optimistic Locking** | `WHERE version = @v AND status = 'available'` | ReservationService |
| **Idempotency** | TransactionRef | Payment events |
| **In-process pub/sub** | `Channel<T>` (bounded) | `TicketStatusHub` |

## 🗄️ Modelo de Datos

### Tablas principales
| Tabla | Descripción |
|-------|-------------|
| `events` | Eventos con nombre y fecha |
| `tickets` | Entradas con estado (`available`, `reserved`, `paid`, `released`) |
| `payments` | Pagos procesados |
| `ticket_history` | Historial de cambios de estado |
| `waitlist_entries` | Inscripciones a lista de espera (índice único `event_id + email` para activos) |
| `waitlist_opportunities` | Oportunidades asignadas con expiración automática |
| `notification_deliveries` | Auditoría de notificaciones enviadas (email) |

### Enums PostgreSQL
- `ticket_status`: available, reserved, paid, released
- `payment_status`: pending, approved, rejected
- `waitlist_entry_status`: active
- `waitlist_opportunity_status`: pending, active, consumed, expired, failed
- `notification_delivery_status`: pending, sent, failed

## 📊 RabbitMQ Topics

| Exchange | Routing Key | Publicado por | Consumido por |
|----------|---|---|---|
| `tickets` | `ticket.reserved` | Producer | ReservationService |
| `tickets` | `ticket.payment.requested` | Producer | PaymentService |
| `tickets` | `ticket.payments.approved` | PaymentService | PaymentService |
| `tickets` | `ticket.payments.rejected` | PaymentService | PaymentService |
| `tickets` | `ticket.status.changed` | ReservationService / PaymentService | CrudService |
| `tickets` | `ticket.released` | PaymentService / ExpireOpportunity | CrudService |
| `tickets` | `ticket.returned_to_inventory` | CrudService | CrudService |
| `tickets` | `waitlist.opportunity.expired` | DLX (auto) | CrudService |

## 🎓 Conceptos Demostrados

### 1. Comunicación Asincrónica
- Requests devuelven 202 Accepted inmediatamente
- Procesamiento ocurre en background
- El Producer no decide resultados de negocio: solo publica eventos

### 2. Consistencia Eventual
- Cada acción genera un evento en RabbitMQ
- Multiple consumers reaccionan de forma independiente
- Frontend se actualiza vía SSE (no polling)

### 3. Desacoplamiento
- Servicios no conocen otros servicios
- Comunicación solo a través de eventos
- Fácil agregar nuevos consumers

### 4. Resiliencia
- Si CRUD Service cae, eventos persisten en RabbitMQ
- Si Producer cae, Frontend recibe error pero puede reintentar
- Transacciones garantizan consistencia
- DLX garantiza expiración automática de oportunidades/reservas

### 5. Domain-Driven Design
- Cada bounded context tiene su propio modelo de dominio
- Regla de dependencia estricta: Domain no referencia a nadie
- Entidades con lógica de negocio (NotificationDelivery state machine, WaitlistOpportunity transitions)

### 6. Observer Pattern (GoF)
- `IEnumerable<IOpportunityObserver>` para notificación multi-canal
- Extensible: agregar un nuevo observer solo requiere implementar la interfaz y registrar en DI

## 📐 Especificaciones (specs/)

Cada HU tiene artefactos de especificación completos generados con speckit:

| Spec | HU | Descripción | Estado |
|------|-----|-------------|--------|
| 001 | Inscripción waitlist | `POST /waitlist/entries` con validaciones | ✅ Implementado |
| 002 | Consulta estado waitlist | `GET /waitlist/entries` con oportunidad activa | ✅ Implementado |
| 003 | Asignación de oportunidad | Observer + DLX delay | ✅ Implementado |
| 004 | Notificación in-app (SSE) | SSE push de oportunidades activadas | ✅ Implementado |
| 005 | Notificación por email | Observer + auditoría + stub sender | ✅ Implementado |
| 006 | Expiración de oportunidad | DLX consumer + liberar ticket | ✅ Implementado |
| 007 | UI inscripción waitlist | Frontend enroll form + validaciones | ✅ Implementado |
| 008 | UI oportunidad waitlist | Frontend claim + countdown + SSE | ✅ Implementado |

## 🔧 Stack Técnico

### Backend
- **.NET 8.0** — Framework
- **Entity Framework Core 8.0.4** — ORM (snake_case via NamingConventions)
- **Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4** — Provider PostgreSQL
- **PostgreSQL 15** — Base de datos
- **RabbitMQ 3.12** — Message broker
- **RabbitMQ.Client 6.8.1** — Driver
- **xUnit 2.4 + NSubstitute 5.1** — Testing
- **Swagger/OpenAPI** — Documentación

### Frontend
- **Next.js** (App Router) — Framework
- **React 19** — UI
- **TypeScript** — Type safety
- **Tailwind CSS** — Estilos
- **shadcn/ui** — Componentes (Card, Button, Input, Label, Badge)
- **sonner** — Notificaciones toast
- **lucide-react** — Iconos
- **Vitest + Testing Library** — Testing

### Infrastructure
- **Docker & Docker Compose** — Containerización
- **PostgreSQL 15** — Persistence
- **RabbitMQ 3.12** — Messaging (con DLX para expiración automática)

## 🤝 Estructura del Proyecto

```
ticketing_project/
├── crud_service/                    # API REST + casos de uso (hexagonal)
│   ├── src/
│   │   ├── CrudService.Domain/              # Entidades, enums, interfaces (puertos)
│   │   ├── CrudService.Application/         # Casos de uso, DTOs, interfaces de app
│   │   ├── CrudService.Infrastructure/      # EF Core, RabbitMQ, observers (adaptadores)
│   │   └── CrudService.Api/                 # Controllers, Program.cs (composition root)
│   └── tests/
│       ├── CrudService.Application.Tests/
│       └── CrudService.Infrastructure.Tests/
├── ReservationService/              # Worker: procesa reservas (hexagonal)
│   ├── src/
│   │   ├── ReservationService.Domain/
│   │   ├── ReservationService.Application/
│   │   ├── ReservationService.Infrastructure/
│   │   └── ReservationService.Worker/
│   └── tests/
│       └── ReservationService.Application.Tests/
├── paymentService/                  # Worker: procesa pagos (hexagonal)
│   ├── src/
│   │   ├── MsPaymentService.Domain/
│   │   ├── MsPaymentService.Application/
│   │   ├── MsPaymentService.Infrastructure/
│   │   └── MsPaymentService.Worker/
│   └── tests/
│       └── MsPaymentService.Application.Tests/
├── producer/                        # API REST: publica eventos a RabbitMQ (hexagonal)
│   ├── src/
│   │   ├── Producer.Domain/
│   │   ├── Producer.Application/
│   │   ├── Producer.Infrastructure/
│   │   └── Producer.Api/
│   └── tests/
│       └── Producer.Application.Tests/
├── frontend/                        # Next.js App Router
│   ├── app/
│   │   ├── events/[id]/             # Detalle evento + waitlist + claim
│   │   ├── buy/                     # Vista comprador
│   │   └── admin/                   # Vista admin
│   ├── components/                  # UI components (shadcn/ui + custom)
│   ├── hooks/                       # Custom hooks (SSE, polling, estado)
│   ├── lib/                         # api.ts, types.ts, utils
│   └── __tests__/                   # Vitest + Testing Library
├── scripts/                         # SQL schema, setup RabbitMQ, datos de prueba
├── specs/                           # Especificaciones por HU (speckit)
│   ├── 001-waitlist-enrollment/
│   ├── 002-waitlist-status-query/
│   ├── 003-waitlist-opportunity-assignment/
│   ├── 004-inapp-notification/
│   ├── 005-email-notification/
│   ├── 006-opportunity-expiration/
│   ├── 007-waitlist-enrollment-ui/
│   └── 008-waitlist-opportunity-ui/
├── docs/                            # Documentación, diagramas, evidencias
├── compose.yml                      # Docker Compose (fuente de verdad operativa)
└── README.md
```

## ⚠️ Lo Que la IA Hizo Mal

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

## 📝 Notas Importantes

1. **Simulación de Pagos**: Los pagos tienen 80% probabilidad de éxito simulada. En producción se integraría con Stripe/PayPal.

2. **Email Sender**: `LogEmailSender` es un stub MVP que solo loguea. En producción se reemplazaría por una implementación SMTP/SendGrid registrando la misma interfaz `IEmailSender`.

3. **SSE**: Frontend abre conexiones `EventSource` para tickets (`/api/tickets/{id}/stream`) y waitlist (`/api/waitlist/stream`). CrudService cierra el stream de tickets tras el primer evento (máx 30s). El stream de waitlist permanece abierto.

4. **CORS**: Producer y CRUD usan `AllowAll` para desarrollo local. Restringir en producción.

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
