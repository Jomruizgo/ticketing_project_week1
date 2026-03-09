# Resumen técnico — Dev B (HU-EXP-01-B)  
## Propagación SSE y robustecimiento del `crud_service`

---

## 1. Contexto del desarrollo

### Historia de Usuario
**HU-EXP-01:** Como plataforma de ticketing, quiero liberar automáticamente reservas vencidas, para que los tickets no queden bloqueados y vuelvan a estar disponibles.

### Alcance de Dev B (ownership)
- `crud_service/Messaging/**` — Consumer de RabbitMQ + Hub SSE
- `crud_service/Controllers/**` — Endpoint SSE `/stream`
- `crud_service/tests/**` — Suite de pruebas completa

> **Regla anti-conflicto:** Dev B NO toca `ReservationService`. El contrato de evento `ticket.status.changed` se congela al inicio.

---

## 2. Metodología TDD aplicada (Red → Green → Refactor)

### Ciclo 1 — Unit Tests (base de la pirámide)

| Fase | Acción | Resultado |
|------|--------|-----------|
| **RED** | Escribí 7 tests nuevos: JSON inválido, payload vacío/nulo, null string, campo faltante, case-insensitive, estados `paid`/`reserved` | 3 tests fallaron (`ProcessMessage` lanzaba `JsonException` en payloads malformados) |
| **GREEN** | Agregué validación defensiva en `ProcessMessage`: check de `null`/vacío antes de deserializar + `try/catch JsonException` | 19/19 ✅ |

**Evidencia RED→GREEN:** El método `ProcessMessage` originalmente no protegía contra JSON inválido. Los 3 tests rojos forzaron la implementación defensiva.

### Ciclo 2 — Component Tests (centro de la pirámide)

| Fase | Acción | Resultado |
|------|--------|-----------|
| **RED→GREEN** | 8 tests de componente: pipeline Consumer→Hub completo (routing por ticketId, multi-subscriber, recuperación post-error, secuencialidad, throughput) | 27/27 ✅ (todos verdes desde el inicio porque la lógica ya existía) |

### Ciclo 3 — Integration Tests (tope de la pirámide)

| Fase | Acción | Resultado |
|------|--------|-----------|
| **RED→GREEN** | 8 tests de integración: contrato SSE (campos camelCase `ticketId`/`status`, formato `data: {...}\n\n`, integridad de datos end-to-end, resiliencia ante mensajes mixtos) | 37/37 ✅ |

### Ciclo 4 — REFACTOR (sin cambiar comportamiento)

| Extracción | Justificación |
|------------|---------------|
| `SseMessageFormatter` (nuevo) | Aislaba la serialización SSE que estaba inline en el Controller → SRP |
| `StatusPayloadParser` (nuevo) | Unificaba el parsing defensivo que estaba privado dentro del Consumer → SRP + reutilización |
| `TicketStatusChangedPayload` (promovido a público) | Era un `private record` del Consumer → ahora es contrato compartido |
| Tests del formatter y parser | 18 tests nuevos garantizan que el refactor no rompió nada → 55/55 ✅ |

### Ciclo 5 — SOLID (Human Check → corrección)

> **Ver sección 5 para detalle del Human Check que originó este ciclo.**

| Fase | Acción | Resultado |
|------|--------|-----------|
| **RED** | 3 tests con `NSubstitute` sobre `ITicketStatusNotifier` mock | Fallaron inicialmente (interfaz no existía) |
| **GREEN** | Creé interfaces `ITicketStatusNotifier` + `ITicketStatusSubscriber`, refactoricé Consumer/Controller para depender de abstracciones | 58/58 ✅ |

---

## 3. Pirámide del Testing

```
              ╔══════════════════╗
              ║   Integration    ║  8 tests
              ║  Contrato SSE    ║  Formato, campos, integridad e2e
              ╠══════════════════╣
              ║    Component     ║  8 tests
              ║ Pipeline C→Hub   ║  Routing, multi-sub, resiliencia
              ╠══════════════════╣
              ║      Unit        ║  42 tests
              ║ Consumer, Hub,   ║  Edge cases, guards, DIP mocks
              ║ Formatter, Parser║
              ╚══════════════════╝
```

**Total: 58 tests en Infrastructure.Tests + 19 en Application.Tests = 77 tests**

### ¿Por qué esta distribución?

| Nivel | Qué valida | Velocidad | Fragilidad |
|-------|-----------|-----------|------------|
| **Unit** (42) | Lógica aislada de cada clase | Milisegundos | Muy baja |
| **Component** (8) | Interacción Consumer↔Hub como pipeline | Milisegundos | Baja |
| **Integration** (8) | Contrato de datos entre servicios y frontend | Milisegundos | Media |

> La pirámide correcta tiene más tests en la base (unit) y menos en la cima (integration), porque los tests de integración son más frágiles y costosos de mantener.

---

## 4. Principios SOLID aplicados

### S — Single Responsibility Principle

| Clase | Responsabilidad única |
|-------|----------------------|
| `TicketStatusConsumer` | Consumir mensajes de RabbitMQ y delegarlos |
| `TicketStatusHub` | Correlacionar suscriptores SSE con ticketId |
| `StatusPayloadParser` | Parsear JSON de payloads de estado defensivamente |
| `SseMessageFormatter` | Serializar `TicketStatusUpdate` al formato SSE del frontend |
| `TicketStatusUpdate` | DTO inmutable para transferencia interna |

### O — Open/Closed Principle

```
Antes:  TicketStatusHub era clase concreta sin abstracción → para cambiar comportamiento
        había que modificar la clase directamente.

Después: ITicketStatusNotifier / ITicketStatusSubscriber permiten agregar nuevas 
         implementaciones (ej. un hub distribuido con Redis) sin modificar el código existente.
```

### L — Liskov Substitution Principle

```csharp
// TicketStatusHub es sustituible por cualquier ITicketStatusNotifier
ITicketStatusNotifier notifier = new TicketStatusHub();      // ✅ funciona
ITicketStatusNotifier notifier = Substitute.For<ITicket...>(); // ✅ funciona en tests
```

Los tests con NSubstitute prueban que el Consumer funciona idénticamente con cualquier implementación de `ITicketStatusNotifier`.

### I — Interface Segregation Principle

```
Antes:  Consumer y Controller dependían de TODA la clase TicketStatusHub
        (Subscribe + Notify), aunque cada uno solo necesitaba UN método.

Después: Interfaces segregadas.
```

```
┌─────────────────────┐     ┌──────────────────────────┐
│  ITicketStatusNotifier │     │ ITicketStatusSubscriber   │
│  + Notify()            │     │ + Subscribe()             │
└─────────┬─────────────┘     └──────────┬───────────────┘
          │                               │
          │    ┌──────────────────┐       │
          └────┤ TicketStatusHub  ├───────┘
               │ implements both  │
               └──────────────────┘
          ▲                               ▲
          │                               │
┌─────────┴──────────┐     ┌──────────────┴────────────┐
│ TicketStatusConsumer│     │ TicketsController          │
│ (solo notifica)     │     │ (solo suscribe)            │
└────────────────────┘     └───────────────────────────┘
```

### D — Dependency Inversion Principle

```
Antes:
  Consumer → TicketStatusHub (concreto)     ❌ Acoplamiento fuerte
  Controller → TicketStatusHub (concreto)   ❌ No testeable sin Hub real

Después:
  Consumer → ITicketStatusNotifier          ✅ Abstracción
  Controller → ITicketStatusSubscriber      ✅ Abstracción
  DI Container resuelve → TicketStatusHub   ✅ Wiring en infraestructura
```

**Registro DI:**
```csharp
services.AddSingleton<TicketStatusHub>();
services.AddSingleton<ITicketStatusNotifier>(sp => sp.GetRequiredService<TicketStatusHub>());
services.AddSingleton<ITicketStatusSubscriber>(sp => sp.GetRequiredService<TicketStatusHub>());
```

---

## 5. Human Check — Correcciones aplicadas al agente

A lo largo del desarrollo, el humano (yo) realicé revisiones y correcciones al código generado por la IA. Estos checkpoints fueron fundamentales para garantizar calidad.

### Human Check #1 — Alcance y enfoque TDD

| Aspecto | Lo que la IA propuso inicialmente | Corrección humana |
|---------|----------------------------------|-------------------|
| **Alcance** | La IA evaluó cobertura de toda la HU-EXP-01 (Dev A + Dev B) e indicó ~35% de cobertura | Corregí el alcance: "sin realizar cambios en las responsabilidades de Dev A, desarrollar únicamente las responsabilidades de Dev B" |
| **Metodología** | La IA iba a implementar directamente sin seguir un orden formal | Exigí explícitamente: "teniendo en cuenta la pirámide del testing (solo unit test, component test, integration test), los principios de calidad y basándonos en TDD" |

**Impacto:** Esto forzó a la IA a seguir la secuencia Red→Green→Refactor por niveles de la pirámide, generando evidencia TDD trazable en cada ciclo.

### Human Check #2 — Validación de principios SOLID

| Aspecto | Lo que la IA entregó | Corrección humana |
|---------|---------------------|-------------------|
| **SOLID** | La IA entregó todo el TDD + pirámide + refactors, pero al auditar los principios SOLID, admitió que **OCP, ISP y DIP no estaban aplicados** | Pregunté: "¿tuviste en cuenta los principios SOLID?" |
| **Diagnóstico** | La IA identificó que Consumer y Controller dependían de la clase concreta `TicketStatusHub`, violando ISP y DIP | — |
| **Resultado** | Se crearon las interfaces segregadas `ITicketStatusNotifier`/`ITicketStatusSubscriber`, se actualizó el DI, y se agregaron 3 tests con NSubstitute para probar DIP | 58/58 ✅ |

**Impacto:** Sin este human check, el código hubiera quedado funcionalmente correcto pero arquitectónicamente acoplado. La corrección mejoró testabilidad, extensibilidad y mantenibilidad.

### ¿Por qué es importante el Human Check?

```
   IA genera código          Humano valida             Código final
   ┌──────────────┐    ┌─────────────────────┐    ┌──────────────────┐
   │ Funcional ✅  │───▶│ ¿SOLID?        ❌   │───▶│ Funcional ✅     │
   │ Tests ✅      │    │ ¿Pirámide?     ✅   │    │ Tests ✅          │
   │ TDD ✅        │    │ ¿Alcance?      ⚠️   │    │ TDD ✅            │
   │ SOLID ❌      │    │ Correcciones ──────▶│    │ SOLID ✅          │
   └──────────────┘    └─────────────────────┘    └──────────────────┘
```

- La IA es eficaz generando volumen de código y tests.
- El humano aporta juicio crítico sobre principios de diseño y alcance.
- Sin la verificación humana, se habrían omitido ISP y DIP.

---

## 6. Principios de calidad adicionales aplicados

| Principio | Dónde se aplica |
|-----------|-----------------|
| **Codificación defensiva** | `StatusPayloadParser.TryParse` retorna null en vez de lanzar excepciones; `ProcessMessage` valida null/vacío/JSON inválido |
| **Inmutabilidad** | `TicketStatusUpdate` y `TicketStatusChangedPayload` son `record` (inmutables por diseño) |
| **Idempotencia** | `Hub.Notify` sin suscriptores = no-op seguro; status vacío = ignorado sin error |
| **Thread-safety** | `ConcurrentDictionary` + `lock` en snapshot de suscriptores |
| **Fail-fast con gracia** | JSON inválido → log warning + return (no crash del BackgroundService) |
| **Contrato congelado** | Payload `{ticketId, newStatus, changedAt}` definido y testeado en contract tests |

---

## 7. Archivos producidos / modificados

### Código de producción
| Archivo | Tipo | Descripción |
|---------|------|-------------|
| `Messaging/ITicketStatusHub.cs` | **Nuevo** | Interfaces ISP: `ITicketStatusNotifier` + `ITicketStatusSubscriber` |
| `Messaging/SseMessageFormatter.cs` | **Nuevo** | Formato SSE centralizado (SRP) |
| `Messaging/StatusPayloadParser.cs` | **Nuevo** | Parsing defensivo unificado (SRP) |
| `Messaging/TicketStatusConsumer.cs` | Modificado | Usa `StatusPayloadParser.TryParse` + depende de `ITicketStatusNotifier` |
| `Messaging/TicketStatusHub.cs` | Modificado | Implementa ambas interfaces |
| `Controllers/TicketsController.cs` | Modificado | Usa `SseMessageFormatter` + depende de `ITicketStatusSubscriber` |
| `DependencyInjection.cs` | Modificado | Registra interfaces segregadas del Hub |

### Tests (58 en Infrastructure.Tests)
| Archivo | Tests | Nivel |
|---------|-------|-------|
| `Messaging/TicketStatusConsumerTests.cs` | 14 | Unit |
| `Messaging/TicketStatusHubTests.cs` | 10 | Unit |
| `Messaging/SseMessageFormatterTests.cs` | 7 | Unit |
| `Messaging/StatusPayloadParserTests.cs` | 11 | Unit |
| `Component/TicketStatusPipelineTests.cs` | 8 | Component |
| `Integration/SseContractIntegrationTests.cs` | 8 | Integration |

---

## 8. Resultados finales

```
Solución completa: 77 tests ✅ — 0 errores
├── CrudService.Application.Tests:      19 tests ✅
└── CrudService.Infrastructure.Tests:   58 tests ✅
    ├── Unit:        42 tests
    ├── Component:    8 tests
    └── Integration:  8 tests
```
