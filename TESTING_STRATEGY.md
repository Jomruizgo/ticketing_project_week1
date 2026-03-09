# TESTING_STRATEGY.md — Estrategia de Calidad Nativa con IA (Semana 3)

> **Principio rector:** _"La IA genera la implementación, el humano garantiza la intención."_
> La suite de pruebas es la única verdad inmutable del sistema. Cada prueba escrita antes del código de producción es un contrato que la IA no puede romper.

---

## 1. QA vs. Testing — Distinción estratégica

| Concepto | Definición operativa en este proyecto |
|---|---|
| **Testing** | Ejecución de scripts automatizados para obtener señal roja/verde: `dotnet test`, reportes `.trx`. |
| **QA** | Estrategia deliberada que responde _qué_, _por qué_ y _cómo_ probar para proteger la lógica de negocio y guiar el diseño mediante TDD. Incluye pirámide de pruebas, clasificación Verificar/Validar, política de mocks y ciclo Red→Green→Refactor. |

**No hacemos testing por cumplimiento.** Cada caso de prueba fue diseñado para responder una pregunta de negocio o técnica concreta antes de existir el código que lo satisface.

---

## 2. Pirámide de Pruebas

```
          [E2E]
       verify-e2e.sh
    verify-devA-expiration.sh
   ─────────────────────────────
      [Integration] — 8 tests
   SseContractIntegrationTests
  ──────────────────────────────
    [Component] — 8 tests
  TicketStatusPipelineTests
 ───────────────────────────────
   [Unit] — 76 tests
  ProcessExpirationCommandHandlerTests (9)
  TicketStatusConsumerTests (14)
  TicketStatusHubTests (10)
  SseMessageFormatterTests (7)
  StatusPayloadParserTests (11)
  MsPaymentService.Worker.Tests (25)
```

**Total: 92 tests automatizados — 100% en verde.**

### Cobertura por nivel

| Nivel | Propósito | Velocidad | Tests |
|---|---|---|---|
| Unit | Probar una clase en aislamiento total (mocks para dependencias) | < 1 ms/test | 76 |
| Component | Sin mocks: pipeline real Consumer→Hub sin infraestructura externa | ~5 ms/test | 8 |
| Integration | Contrato SSE end-to-end con serialización JSON real | ~10 ms/test | 8 |
| E2E | Flujo completo sobre Docker Compose con RabbitMQ y PostgreSQL reales | segundos | scripts |

---

## 3. Ciclo TDD IA-Native — Red → Green → Refactor

### 3.1 Dev A — ReservationService (commits atómicos)

El historial de git evidencia el ciclo completo por commit:

| Fase | Commit | Descripción |
|---|---|---|
| 🔴 RED | `f676b8a` — `test(red): add expiration use case tests including technical failure` | Tests escritos antes de existir `ProcessExpirationCommandHandler`. Fallaban intencionalmente al crearse. |
| 🟢 GREEN | `83eccf1` — `feat(green): implement automatic reservation expiration flow` | Código mínimo para hacer pasar todos los tests del commit anterior. |
| 🔵 REFACTOR | `3f4638c` — `refactor: decouple status publishing and harden expiration consumer` | Extracción de `IStatusChangedPublisher`, mejora de logs y hardening del consumer. Sin romper ningún test. |
| ✅ E2E | `9ba2bcd` — `test(integration): add repeatable docker verification for Dev A expiration flow` | Script reproducible de verificación con Docker Compose. |

**Verificación:** `git log --oneline f676b8a 83eccf1 3f4638c 9ba2bcd`

### 3.2 Dev B — CrudService (ciclo documentado en rama)

Los cambios de Dev B están en `feature/mock-imposible/crud-tests-em` (PR #18, commit `0703302`). El proceso TDD fue ejecutado en la iteración pero consolidado en un commit único por decisión de alcance (rama personal de trabajo). La secuencia fue:

| Fase | Descripción | Evidencia |
|---|---|---|
| 🔴 RED | Tests escritos para `TicketStatusConsumer.ProcessMessage`, `ITicketStatusNotifier` (DIP), `SseMessageFormatter`, `StatusPayloadParser`. Todos fallaban porque el código de producción no existía. | 58 tests en rama antes de modificar production code |
| 🟢 GREEN | Implementación de `SseMessageFormatter`, `StatusPayloadParser`, interfaces ISP `ITicketStatusNotifier`/`ITicketStatusSubscriber`, reescritura de `TicketStatusConsumer` con DIP, y migración hexagonal de `CrudService`. | Commit `0703302` en PR #18 |
| 🔵 REFACTOR | Extracción de responsabilidades: `SseMessageFormatter` (SRP), `StatusPayloadParser` (SRP), segregación de interfaces ISP, `DependencyInjection.cs` con factories para las abstracciones. | Incluido en mismo commit `0703302` |

**Declaración de transparencia:** Los commits de Dev B no quedaron atómicos por fase en el historial de `develop`. La secuencia RED→GREEN→REFACTOR fue real e iterativa, pero se consolidó antes del PR para mantener un diff limpio y legible para revisión de pares.

---

## 4. Verificar vs. Validar

> **Verificar** = "¿está técnicamente bien construido?" — lógica técnica, contratos, parsing, flujo.
> **Validar** = "¿protege una regla de negocio crítica?" — transiciones de estado, idempotencia, invariantes.

### Tabla completa de casos

| Tipo | Caso | Test(s) responsables | Riesgo que evita |
|---|---|---|---|
| **Verificar** | `StatusPayloadParser` retorna `null` para JSON inválido, vacío o `null` literal | `StatusPayloadParserTests` (11 casos) | Caída del consumer por payload malformado; excepciones no controladas que interrumpen el pipeline de mensajería |
| **Verificar** | `SseMessageFormatter.ToSseJson` emite `ticketId`/`status` en camelCase sin exponer `newStatus` | `SseMessageFormatterTests` (7 casos) | Ruptura del contrato SSE con el frontend; eventos no parseables por el cliente |
| **Verificar** | `TicketStatusConsumer.ProcessMessage` llama a `ITicketStatusNotifier.Notify` con argumentos correctos | `TicketStatusConsumerTests` — tests DIP con mock (3 casos) | Desconexión silenciosa entre consumer y hub; pérdida de eventos sin error visible |
| **Verificar** | `TicketStatusHub.Subscribe` devuelve `ChannelReader` no nulo; distintos tickets son aislados | `TicketStatusHubTests` (10 casos) | Suscriptores que reciben eventos de tickets ajenos (cross-contamination) |
| **Verificar** | Pipeline Consumer→Hub rutea correctamente mensajes concurrentes para 20 tickets distintos | `TicketStatusPipelineTests` (8 casos) | Pérdida de eventos o entregas cruzadas bajo carga ligera |
| **Verificar** | Contrato SSE end-to-end: campo `status` (no `newStatus`), `ticketId` como `long`, solo 2 campos | `SseContractIntegrationTests` (8 casos) | Cambio silencioso de contrato JSON que rompe el frontend en producción |
| **Validar** | Ticket `reserved` vencido transiciona a `released` y publica `ticket.status.changed` | `ProcessExpirationCommandHandlerTests.Handle_WhenTicketReserved_ReturnsSuccess` | Bloqueo indefinido de inventario; tickets no disponibles para nuevas compras |
| **Validar** | Ticket `paid` **no** se revierte por expiración tardía | `ProcessExpirationCommandHandlerTests.Handle_WhenTicketPaid_ReturnsNoOp` | Inconsistencia de negocio crítica: venta revertida después de cobrarse |
| **Validar** | Expiración duplicada (ticket ya `released`) no genera doble transición | `ProcessExpirationCommandHandlerTests.Handle_WhenTicketAlreadyReleased_ReturnsNoOp` | Efectos duplicados por reentrega de mensajes RabbitMQ (at-least-once delivery) |
| **Validar** | Fallo técnico de persistencia no cambia estado del ticket | `ProcessExpirationCommandHandlerTests.Handle_WhenRepositoryFails_*` | Estado inconsistente entre base de datos y mensajes publicados |
| **Validar** | Notificación SSE de `released` llega al suscriptor activo (contrato E2E pipeline) | `SseContractIntegrationTests.SseOutput_StatusValues_MatchExpectedJsonContract` | El frontend no recibe notificación de liberación; UX degradada |

---

## 5. Estrategia de Mocks

### Política de uso

| Situación | Decisión | Razón |
|---|---|---|
| Test unitario de caso de uso (`ProcessExpirationCommandHandler`) | Mock de `ITicketRepository` y `IStatusChangedPublisher` | Aislar la lógica de negocio de la infraestructura (EF, RabbitMQ); verificar decisión, no implementación |
| Test unitario de `TicketStatusConsumer` con DIP | Mock de `ITicketStatusNotifier` via `NSubstitute` | Probar que el consumer depende de la abstracción, no del `TicketStatusHub` concreto |
| Test unitario de `TicketStatusHub` | Sin mocks — clase pura en memoria con `Channel<T>` | La clase opera en memoria; aislarla con mock sería contra-productivo |
| Component test (`TicketStatusPipelineTests`) | Sin mocks — Consumer real + Hub real | Verificar integración del pipeline sin infraestructura externa |
| Integration test de contrato SSE | Sin mocks — serialización JSON real | Validar el contrato byte a byte tal como lo recibirá el frontend |

### Librería y patrón

```csharp
// DIP proof: consumer funciona con CUALQUIER ITicketStatusNotifier
var mockNotifier = Substitute.For<ITicketStatusNotifier>();
consumer.ProcessMessage("""{"ticketId":42,"newStatus":"released",...}""");
mockNotifier.Received(1).Notify(42, "released"); // verifica contrato de la interfaz
```

NSubstitute 5.1.0 — seleccionado por API fluida que permite aserciones de interacción (`Received`, `DidNotReceive`, `Arg.Any<T>`) sin violar el principio de prueba por comportamiento.

---

## 6. Cobertura E2E

Los scripts en `scripts/` cubren el flujo de negocio completo sobre infraestructura real (Docker Compose):

| Script | Flujo cubierto | Entorno |
|---|---|---|
| `scripts/verify-devA-expiration.sh` | Reservar ticket → esperar TTL (~5 min) → verificar transición a `released` en PostgreSQL | Docker Compose local |
| `scripts/verify-e2e.sh` | Buscar ticket disponible → reservar → pagar → verificar `paid` via SSE → verificar persistencia | Docker Compose local |

**Comando de reproducción:**
```bash
docker compose up -d
sleep 10  # esperar healthchecks
bash scripts/verify-e2e.sh
bash scripts/verify-devA-expiration.sh
```

---

## 7. Integridad de la Suite — Zero Errors

| Módulo | Suite | Resultado | Reporte |
|---|---|---|---|
| ReservationService | `Application.Tests` | ✅ 9/9 verde | `docs/evidencias/semana3/artifacts/reservation/reservation-application-week3.trx` |
| CrudService | `Infrastructure.Tests` | ✅ 58/58 verde | `docs/evidencias/semana3/artifacts/crud/crud-week3.trx` |
| PaymentService | `Worker.Tests` | ✅ 25/25 verde | `docs/evidencias/semana3/artifacts/payment/payment-week3.trx` |
| **Total** | | **✅ 92/92 verde** | |

**Regresiones detectadas: 0**

### Comandos reproducibles

```bash
# ReservationService
dotnet test ReservationService/tests/ReservationService.Application.Tests/ \
  --nologo --logger "trx;LogFileName=reservation-application-week3.trx" \
  --results-directory docs/evidencias/semana3/artifacts/reservation

# CrudService (requiere rama feature/mock-imposible/crud-tests-em o merge de PR #18)
dotnet test crud_service/CrudService.sln \
  --nologo --logger "trx;LogFileName=crud-week3.trx" \
  --results-directory docs/evidencias/semana3/artifacts/crud

# PaymentService
dotnet test paymentService/MsPaymentService.sln \
  --nologo --logger "trx;LogFileName=payment-week3.trx" \
  --results-directory docs/evidencias/semana3/artifacts/payment
```

---

## 8. Arquitectura de Pruebas y Hexagonal

Las pruebas respetan y refuerzan la Arquitectura Hexagonal:

- **Domain layer**: sin dependencias externas — testeable con clases POCO puros.
- **Application layer**: casos de uso probados con mocks de puertos (`ITicketRepository`, `IStatusChangedPublisher`). Los tests de `ProcessExpirationCommandHandlerTests` nunca tocan EF Core ni RabbitMQ.
- **Infrastructure layer**: `TicketStatusConsumer`, `TicketStatusHub`, `SseMessageFormatter`, `StatusPayloadParser` — testeados independientemente con `NSubstitute` y sin base de datos.
- **E2E**: sobre Docker Compose, nunca en tests de xUnit (separa velocidad de cobertura estructural).

---

## 9. AI-Native: cómo se guió a la IA

| Anti-patrón evitado | Patrón aplicado |
|---|---|
| "Escribe código para esta función" → después "hazle pruebas a este archivo" | Pruebas escritas primero (RED); la IA completó la implementación guiada por los contratos de las pruebas |
| Mocks que replican la implementación (frágiles) | Mocks que verifican contratos de interfaz con `Received`/`DidNotReceive` |
| Tests que siempre pasan sin aserción real ("teatro de calidad") | Cada test tiene al menos una aserción de comportamiento observable |
| Un único test "happy path" por módulo | Cobertura de edge cases: JSON malformado, `null`, vacío, estado inválido, duplicados, concurrencia |

---

## 10. Declaración de alcance

- `ReservationService.Domain.Tests` e `ReservationService.Infrastructure.Tests` existen como proyectos de test pero sin casos implementados en este sprint. No es fallo del runner — `dotnet test --list-tests` confirma cero casos discoverables. El alcance de pruebas de ReservationService para Semana 3 se concentró en `Application.Tests` (lógica de negocio de expiración).
- La suite de PR #18 (58 tests de CrudService) está disponible en la rama `feature/mock-imposible/crud-tests-em` y en `docs/evidencias/semana3/artifacts/crud/crud-week3.trx`. Pendiente de merge a `develop`.
