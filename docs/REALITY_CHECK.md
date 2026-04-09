# Reality Check — TicketRush MVP

**Fecha:** 2026-04-09  
**Método:** Cruce exhaustivo de specs (001–008) contra código, infraestructura, tests y documentación.

---

## 1. Features declaradas vs. estado real

| # | Feature | Backend | Frontend | Tests | RabbitMQ | Estado real |
|---|---------|---------|----------|-------|----------|-------------|
| 001 | Waitlist Enrollment | ✅ `POST /api/waitlist/entries` en CrudService | ✅ `WaitlistEnrollForm` | ✅ Unit + Integration | N/A | **Funcional** |
| 002 | Waitlist Status Query | ✅ `GET /api/waitlist/entries?eventId&email` | ✅ `WaitlistStatus` | ✅ Unit | N/A | **Funcional** |
| 003 | Opportunity Assignment | ✅ `TicketReleasedConsumer` + `AssignOpportunityHandler` | N/A (backend puro) | ✅ Unit | ✅ `ticket.released` → `opportunity.activated` | **Funcional** |
| 004 | In-App Notification (SSE) | ✅ `GET /api/waitlist/stream` + `SseNotificationConsumer` + `WaitlistSseHub` | ✅ `useWaitlistSse` hook | ✅ Unit + Integration | ✅ `opportunity.activated` → `q.sse.notification` | **Funcional** |
| 005 | Email Notification | ⚠️ `EmailNotificationObserver` + `notification_deliveries` tabla | N/A | ✅ Unit | N/A (observer in-process) | **Parcial — sender es STUB** |
| 006 | Opportunity Expiration | ✅ `WaitlistOpportunityExpiredConsumer` + `ExpireOpportunityHandler` + DLX | N/A (backend puro) | ✅ Unit + Integration | ✅ DLX `q.waitlist.opportunity.delay` → `q.waitlist.opportunity.expired` | **Funcional** |
| 007 | Waitlist Enrollment UI | N/A (usa backend 001) | ✅ `WaitlistEnrollForm` integrado en `/buy/[id]` | ✅ Component tests | N/A | **Funcional** |
| 008 | Waitlist Opportunity UI | ✅ `POST /api/waitlist/opportunities/{id}/claim` | ✅ `WaitlistStatus` con countdown + claim | ✅ Unit + Integration | Consume SSE de 004 | **Funcional** |

### Veredicto general de features
- **7/8 features completamente funcionales.**
- **1/8 parcial** (005): la auditoría y el observer existen, pero `LogEmailSender` solo loguea — no envía correo real. Aceptable para MVP si se documenta; bloqueante para producción.

---

## 2. Contratos de API: spec vs. realidad

### 2.1 Endpoints — backend vs. frontend

| Endpoint backend (CrudService) | Función en `api.ts` | ¿Coinciden? |
|---|---|---|
| `GET /api/events` | `getEvents()` → `${CRUD_URL}/api/events` | ✅ |
| `GET /api/events/{id}` | `getEvent(id)` → `${CRUD_URL}/api/events/${id}` | ✅ |
| `POST /api/events` | `createEvent()` | ✅ |
| `PUT /api/events/{id}` | `updateEvent()` | ✅ |
| `DELETE /api/events/{id}` | `deleteEvent()` | ✅ |
| `GET /api/tickets/by-event/{eventId}` | `getTicketsByEvent()` → `/api/tickets/event/${eventId}` | ⚠️ **Discrepancia potencial** |
| `GET /api/tickets/{id}` | `getTicket(id)` | ✅ |
| `POST /api/tickets` | `createTickets()` → `/api/tickets/bulk` | ⚠️ **Discrepancia potencial** |
| `PUT /api/tickets/{id}/status` | `updateTicketStatus()` | ✅ |
| `POST /api/tickets/reserve` (Producer) | `reserveTicket()` | ✅ |
| `POST /api/payments/process` (Producer) | `processPayment()` | ✅ |
| `POST /api/waitlist/entries` | `enrollInWaitlist()` | ✅ |
| `GET /api/waitlist/entries?eventId&email` | `getWaitlistStatus()` | ✅ |
| `POST /api/waitlist/opportunities/{id}/claim` | `claimOpportunity()` | ✅ |
| `GET /api/waitlist/stream?email` | `useWaitlistSse` hook (EventSource) | ✅ |

**Hallazgos de discrepancia:**
1. **Tickets by event**: Backend declara `/api/tickets/by-event/{eventId}` pero frontend llama `/api/tickets/event/${eventId}`. **Si el sistema funciona, el controller acepta ambas rutas o la documentación del backend está desactualizada.** Requiere verificación manual.
2. **Crear tickets**: Backend declara `POST /api/tickets` pero frontend llama `POST /api/tickets/bulk`. Misma situación — verificar ruta real en el controller.

### 2.2 DTOs — `types.ts` vs. backend

| Tipo frontend | Propiedades | ¿Coincide con backend? |
|---|---|---|
| `Event` | `id, name, startsAt, availableTickets, reservedTickets, paidTickets` | ✅ Consistente con DTO del controller |
| `Ticket` | `id, eventId, status, reservedAt, expiresAt, paidAt, orderId, reservedBy, version` | ✅ Con normalización a minúsculas |
| `WaitlistEntryDto` | `id, eventId, buyerEmail, status, enrolledAt` | ✅ Matching 1:1 |
| `WaitlistOpportunityDto` | `id, ticketId, status, activatedAt, expiresAt, remainingMinutes` | ✅ Matching 1:1 |
| `ClaimOpportunityResponse` | `opportunityId, ticketId, eventId, status` | ✅ |
| `WaitlistStatusResponse` | `entry + opportunity?` | ✅ |

**Normalización de enums:** Frontend aplica `.toLowerCase()` a `ticket.status`. Los enums PostgreSQL están en minúsculas (`available`, `reserved`…); los enums C# en el CrudService usan convención PascalCase que se serializa como minúsculas via `JsonStringEnumConverter`. La normalización del frontend es una capa de seguridad redundante pero no incorrecta.

---

## 3. RabbitMQ: topología declarada vs. consumida

### 3.1 Colas declaradas en `setup-rabbitmq.sh` (22 pasos)

| Cola | Routing Key (binding) | TTL/DLX | ¿Consumidor implementado? |
|---|---|---|---|
| `q.ticket.reserved` | `ticket.reserved` | — | ✅ `RabbitMQConsumer` (ReservationService) |
| `q.ticket.payments.approved` | `ticket.payments.approved` | — | ✅ `TicketPaymentConsumer` (PaymentService) |
| `q.ticket.payments.rejected` | `ticket.payments.rejected` | — | ✅ `TicketPaymentConsumer` (PaymentService) |
| `q.ticket.payment.requested` | `ticket.payment.requested` | — | ✅ `TicketPaymentConsumer` (PaymentService) |
| `q.ticket.status.changed` | `ticket.status.changed` | — | ✅ `TicketStatusConsumer` (CrudService) |
| `q.ticket.expired` | `ticket.expired` | — | ✅ `TicketExpiredConsumer` (ReservationService) |
| `q.ticket.reserved.delay` | DLX → `ticket.expired` | TTL 5min | ✅ Populada por ReservationService al reservar |
| `q.ticket.released` | `ticket.released` | — | ✅ `TicketReleasedConsumer` (CrudService) |
| `q.ticket.returned` | `ticket.returned_to_inventory` | — | ⚠️ **Sin consumidor explícito** |
| `q.waitlist.opportunity.delay` | DLX → `waitlist.opportunity.expired` | TTL 15min (configurable) | ✅ Populada por `AssignOpportunityHandler` |
| `q.waitlist.opportunity.expired` | `waitlist.opportunity.expired` | — | ✅ `WaitlistOpportunityExpiredConsumer` (CrudService) |

**Hallazgos:**
1. **`q.ticket.returned`** está declarada en `setup-rabbitmq.sh` con binding a `ticket.returned_to_inventory`, pero no se encontró un consumidor dedicado para ella. Los mensajes se publican (por `ExpireOpportunityHandler` cuando no hay elegibles), pero no se procesan. **Esto podría ser intencional** (cola de auditoría/observabilidad) o una feature incompleta (devolver ticket a inventario general). **Riesgo: mensajes acumulándose sin consumir.**

2. **Cola SSE `q.sse.notification`**: La cola para notificaciones SSE (`SseNotificationConsumer`) **no aparece en `setup-rabbitmq.sh`**. El consumer la declara programáticamente con nombre auto-generado (patrón fanout por instancia). Esto es correcto por diseño (cada instancia tiene cola exclusiva), pero está **indocumentado** en el script centralizado.

3. **Falta el routing key `opportunity.activated` en el script de setup**: No hay binding explícito para `opportunity.activated` en `setup-rabbitmq.sh`, porque las colas SSE se crean dinámicamente. Funciona, pero puede confundir al leer el script de setup como "fuente de verdad" de la topología.

---

## 4. Base de datos: `schema.sql` vs. modelos EF

### 4.1 Tablas

| Tabla en `schema.sql` | Entidad en EF Core | DbContext | ¿Alineados? |
|---|---|---|---|
| `events` | `Event` | ✅ CrudService, PaymentService, ReservationService | ✅ |
| `tickets` | `Ticket` | ✅ Todos los servicios | ✅ |
| `payments` | `Payment` | ✅ CrudService, PaymentService | ✅ |
| `ticket_history` | `TicketHistory` | ✅ CrudService, PaymentService | ✅ |
| `waitlist_entries` | `WaitlistEntry` | ✅ CrudService | ✅ |
| `waitlist_opportunities` | `WaitlistOpportunity` | ✅ CrudService | ✅ |
| `notification_deliveries` | `NotificationDelivery` | ✅ CrudService | ✅ |

### 4.2 Enums PostgreSQL

| Enum | Valores en schema.sql | Valores en C# | ¿Consistentes? |
|---|---|---|---|
| `ticket_status` | available, reserved, paid, released, cancelled | ✅ Matching | ✅ |
| `payment_status` | pending, approved, failed, expired | ✅ Matching | ✅ |
| `waitlist_entry_status` | active, consumed, expired | ✅ Matching | ✅ |
| `waitlist_opportunity_status` | pending, active, consumed, expired, failed | ✅ Matching | ✅ |
| `notification_delivery_status` | pending, sent, failed | ✅ Matching | ✅ |

### 4.3 Índices de integridad

| Índice | Tabla | Propósito | ¿Implementado? |
|---|---|---|---|
| `idx_tickets_status_expires_at` | `tickets` | Performance en queries de expiración | ✅ |
| `idx_waitlist_entries_active_unique` | `waitlist_entries` | Unicidad `(event_id, email)` donde `status = active` | ✅ |
| `idx_waitlist_opportunities_active_ticket_unique` | `waitlist_opportunities` | Una sola oportunidad activa por ticket | ✅ |

**Veredicto:** Schema y modelos EF están **completamente alineados**.

---

## 5. Infraestructura (`compose.yml`)

### 5.1 Health checks

| Servicio | ¿Tiene health check? | Comando |
|---|---|---|
| postgres | ✅ | `pg_isready` |
| rabbitmq | ✅ | `rabbitmq-diagnostics ping` |
| producer | ✅ | `curl /health` |
| crud-service | ✅ | `curl /health` |
| payment | ❌ | — |
| reservation-service | ❌ | — |
| frontend | ❌ | — |

**Hallazgos:**
- **Payment y Reservation** son workers sin health check. Si un worker muere silenciosamente, Docker lo reinicia (`restart: unless-stopped`) pero no hay manera de consultare su estado desde fuera. Para MVP es aceptable; para producción necesitan health checks o al menos liveness probes.
- **Frontend** sin health check. Menor prioridad (el usuario nota visualmente si no carga).

### 5.2 Variables de entorno

- **Credenciales DB**: Referenciadas via `${POSTGRES_USER}`, `${POSTGRES_PASSWORD}` — requieren `.env` file. No se encontró `.env.example` documentado. **Riesgo: onboarding sin documentar.**
- **CORS**: `AllowAll` en producer y crud-service. Documentado como intencional para MVP. OK.
- **WAITLIST_OPPORTUNITY_TTL_MS**: Configurable desde compose → `setup-rabbitmq.sh`. Correcto.

### 5.3 Orden de arranque

```
postgres (healthy) ──┐
                     ├─→ rabbitmq-setup (completed) ─→ producer
rabbitmq (healthy) ──┘                               ─→ payment
                                                      ─→ crud-service ─→ frontend
                                                      ─→ reservation-service
```

**Correcto.** Las dependencias de arranque son coherentes. `rabbitmq-setup` asegura que la topología de colas/exchanges exista antes de que arranquen los servicios.

---

## 6. Tests: cobertura real

| Servicio | Proyectos de test | Archivos | Scope | Pass verificado |
|---|---|---|---|---|
| CrudService | 3 (Api, Application, Infrastructure) | ~21 archivos | Unit + Integration + Endpoint | ✅ 19/19 (última ejecución documentada) |
| PaymentService | 1 (Application) | ~6 archivos | Unit | ✅ 25/25 |
| ReservationService | 3 (Application, Domain, Infrastructure) | ~4 archivos | Unit + Config | ✅ 4/4 |
| Producer | 1 (Application) | ~2 archivos | Unit | ✅ |
| Frontend | 1 (Vitest) | ~3+ archivos | Component + API | Sin conteo verificado |

### Tests ausentes (riesgos)

| Gap | Impacto | Severidad |
|---|---|---|
| **Sin tests E2E de integración cross-service** | No se verifica el flujo completo reserva → pago → estado. Solo hay scripts bash (`verify-e2e.sh`) | ⚠️ Media |
| **Sin tests de concurrencia** | Spec 001 menciona "dos inscripciones simultáneas del mismo email" como caso de test; no se encontró implementado | ⚠️ Media |
| **Workers (Payment, Reservation) sin tests de consumer reales** | Los consumers están acoplados a RabbitMQ (DIP violation documentada). Solo se testean los handlers, no el wiring del consumer | ⚠️ Media (documentada en DEBT_REPORT) |
| **Sin tests de carga/stress** | Specs 004/006 mencionan límites de conexiones SSE y TTL; no hay tests de carga | ℹ️ Baja (MVP) |

---

## 7. Seguridad

| Aspecto | Estado | Severidad |
|---|---|---|
| **CORS `AllowAll`** | Habilitado en producer y crud-service. Intencional para MVP local. | ℹ️ Baja (MVP) / 🔴 Crítica (producción) |
| **Sin autenticación** | Ningún endpoint requiere auth. El identificador de comprador es `email` en texto plano. | ℹ️ Esperado (MVP) / 🔴 Crítica (producción) |
| **Validación de inputs** | Email validado RFC 5321 simplificado en backend + frontend. Payloads validados con DTOs tipados. | ✅ Aceptable |
| **SQL Injection** | EF Core parametriza queries. `TicketRepository` en PaymentService usa `FromSqlRaw()` — potencial riesgo si los parámetros no están parametrizados. | ⚠️ Verificar `FromSqlRaw()` |
| **Secrets en compose** | Referenciados via `${VAR}` (env file). No hay secrets hardcodeados en compose.yml. | ✅ |
| **Secrets en código** | No se encontraron credentials hardcodeadas. | ✅ |
| **Rate limiting** | Solo en SSE (`SSE_MAX_CONNECTIONS_PER_EMAIL`). Sin rate limiting en endpoints REST. | ⚠️ Media (producción) |
| **HTTPS** | No configurado — todo HTTP en dev. | ℹ️ Esperado (dev) |

---

## 8. Deuda técnica confirmada

### Crítica (bloqueante para producción)

| # | Deuda | Ubicación | Impacto |
|---|---|---|---|
| 1 | **Email sender es stub** — `LogEmailSender` solo loguea, no envía correo real | `crud_service/.../Services/LogEmailSender.cs` | Feature 005 incompleta para producción |
| 2 | **DIP violation en ReservationService** — consumer instancia `ConnectionFactory` directamente, imposible de testear unitariamente | `ReservationService/.../Messaging/RabbitMQConsumer.cs` | Tests de consumer imposibles sin RabbitMQ |
| 3 | **DIP violation en PaymentService** — `TicketStateService` depende de `PaymentDbContext` concreto | `paymentService/.../Services/TicketStateService.cs` | Tests requieren DB real |
| 4 | **`FromSqlRaw()` sin verificar parametrización** | `paymentService/.../Repositories/TicketRepository.cs` | Riesgo potencial de SQLi |

### Moderada (no bloqueante, pero genera fricción)

| # | Deuda | Ubicación |
|---|---|---|
| 5 | SRP violation — `TicketStateService` mezcla transacciones + lógica de estados + historial | `paymentService/.../Services/TicketStateService.cs` |
| 6 | SRP violation — `TicketReservationConsumer.ExecuteAsync()` con 5 responsabilidades | `ReservationService/.../Consumers/TicketReservationConsumer.cs` |
| 7 | Código duplicado — patrón serialize+publish en `RabbitMQTicketPublisher` y `RabbitMQPaymentPublisher` | `producer/.../Messaging/` |
| 8 | Magic number — `AddMinutes(5)` hardcodeado en `PaymentValidationService` | `paymentService/.../Services/PaymentValidationService.cs` |
| 9 | No determinismo — `Random.Shared.Next()` en simulación de pago (no testeable) | `paymentService` |
| 10 | `q.ticket.returned` sin consumidor — mensajes acumulándose | `setup-rabbitmq.sh` + ningún consumer |

### Leve (cosméticas / organizativas)

| # | Deuda | Ubicación |
|---|---|---|
| 11 | 4 repos en un solo archivo (`RepositoriesImplementation.cs`) | `paymentService` |
| 12 | 4 interfaces en un archivo (`IRepositories.cs`) | `paymentService` |
| 13 | Numeración inconsistente de pasos en `setup-rabbitmq.sh` (empieza `[5/12]` pero termina en `[22/22]`) | `scripts/setup-rabbitmq.sh` |
| 14 | Sin `.env.example` documentado para onboarding | Raíz del repo |

---

## 9. Riesgos para producción

| Riesgo | Probabilidad | Impacto | Mitigación mínima |
|---|---|---|---|
| **Sin autenticación** | Cierta | Crítico | Implementar auth (JWT, API keys) antes de exponer |
| **CORS `AllowAll`** | Cierta | Alto | Restringir origins al dominio de producción |
| **Email no funcional** (stub) | Cierta | Medio | Implementar adapter SMTP real (`IEmailSender`) |
| **Workers sin health check** | Media | Medio | Agregar endpoint `/health` o liveness probe |
| **Sin rate limiting en REST** | Media | Medio | Middleware de rate limiting por IP/email |
| **`q.ticket.returned` sin consumidor** | Baja (cola crece) | Bajo | Implementar consumer o TTL en la cola |
| **`FromSqlRaw()` sin auditar** | Baja | Alto | Auditar queries parametrizadas |

---

## 10. Resumen ejecutivo

| Dimensión | Calificación | Nota |
|---|---|---|
| **Completitud funcional** | 🟢 7.5/8 | Solo falta email real (005) |
| **Alineación spec ↔ código** | 🟢 Alta | Contratos coherentes; 2 rutas de tickets a verificar |
| **Infraestructura** | 🟢 Sólida | compose + schema + RabbitMQ bien amarrados |
| **Tests** | 🟡 Aceptable (MVP) | 48+ tests unitarios, sin E2E automatizado cross-service |
| **Seguridad** | 🔴 MVP only | Sin auth, sin rate limiting, CORS abierto |
| **Deuda técnica** | 🟡 Controlada | 14 items documentados, 4 críticos para producción |
| **Preparación para producción** | 🔴 No listo | Requiere auth, CORS, email real, health checks de workers |

**El sistema es un MVP funcional y coherente.** La arquitectura hexagonal está bien aplicada en CrudService. Los servicios legacy (Payment, Reservation) tienen acoplamiento infraestructural documentado. La topología async (RabbitMQ + DLX + SSE) funciona end-to-end. Para producción, los bloqueantes son seguridad y el stub de email.
