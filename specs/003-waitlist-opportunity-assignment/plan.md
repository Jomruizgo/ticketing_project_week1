# Implementation Plan: Asignación de Oportunidad de Lista de Espera

**Branch**: `003-waitlist-opportunity-assignment` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-waitlist-opportunity-assignment/spec.md`

## Summary

Cuando una entrada queda libre (reserva expirada o pago rechazado), el sistema la detecta mediante un consumer RabbitMQ (`ticket.released`), selecciona al siguiente comprador elegible de la lista de espera usando FIFO, crea una oportunidad `pending`, intenta reservar temporalmente la entrada, y transiciona a `active` si la reserva tiene éxito o a `failed` si falla. Si no hay elegibles, la entrada vuelve al inventario general. El consumer opera con `prefetchCount = 1` para procesamiento secuencial.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1, RabbitMQ.Client  
**Storage**: PostgreSQL (tabla `waitlist_opportunities` existente, columnas snake_case)  
**Testing**: xUnit 2.4.2, NSubstitute 5.1.0; Testcontainers para integración  
**Target Platform**: Linux server (Docker)  
**Project Type**: Worker consumer dentro del CRUD Service (microservicio existente)  
**Performance Goals**: Oportunidad creada y entrada reservada en < 5s desde recepción del mensaje  
**Constraints**: `prefetchCount = 1` (secuencial); protección de unicidad a nivel de BD como segunda línea de defensa  
**Scale/Scope**: Volumen bajo de `ticket.released` (solo ocurre cuando reserva expira o pago falla)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Estado | Evidencia |
|-----------|--------|-----------|
| I. Hexagonal + DDD | ✅ PASS | Domain define puertos (`IPrioritizationStrategy`, `ITicketReservationPort`, `IOpportunityObserver`); Infrastructure implementa adaptadores (`FifoStrategy`, consumer RabbitMQ). Domain no referencia frameworks. |
| II. Async-First | ✅ PASS | El flujo es 100% asíncrono: consumer RabbitMQ → caso de uso → publicación de eventos. Sin endpoints HTTP síncronos para esta feature. |
| III. TDD Strict | ✅ PASS | TC-HU3-01 a TC-HU3-04 definen la fase RED. Tests unitarios del handler primero, luego implementación GREEN. |
| IV. Testing Pyramid | ✅ PASS | Unitarias (NSubstitute) para handler. Integración (Testcontainers) para FIFO query y restricción única. |
| V. SOLID | ✅ PASS | SRP: handler orquesta, strategy selecciona, port reserva. OCP: Strategy extensible sin modificar handler. DIP: handler depende de abstracciones (puertos). ISP: cada puerto define una responsabilidad. |
| VI. GoF-Only | ✅ PASS | Strategy (priorización), Observer (notificación post-activación), State (transiciones de oportunidad), Command (AssignOpportunityCommand). |
| VII. Zero AI Smells | ✅ PASS | Nombres intencionales: `AssignOpportunityHandler`, `FifoStrategy`, `TicketReleasedConsumer`. |
| VIII. Language Convention | ✅ PASS | Código en inglés, documentación en español. |

**GATE PASSED** — 8/8 principios cumplidos.

## Project Structure

### Documentation (this feature)

```text
specs/003-waitlist-opportunity-assignment/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
crud_service/src/
├── CrudService.Domain/
│   ├── Entities/
│   │   ├── WaitlistOpportunity.cs        # Ya existe — agregar método TransitionTo
│   │   └── WaitlistEntry.cs              # Ya existe
│   ├── Enums/
│   │   ├── WaitlistOpportunityStatus.cs  # Ya existe (Pending, Active, Consumed, Expired, Failed)
│   │   └── WaitlistEntryStatus.cs        # Ya existe
│   ├── Interfaces/
│   │   ├── IWaitlistOpportunityRepository.cs  # Ya existe — extender con AddAsync, FindActiveByTicketIdAsync
│   │   ├── IWaitlistEntryRepository.cs        # Ya existe — extender con GetActiveEntriesByEventAsync, UpdateStatusAsync
│   │   ├── IPrioritizationStrategy.cs         # NUEVO — puerto Strategy
│   │   ├── ITicketReservationPort.cs          # NUEVO — puerto para reserva temporal
│   │   └── IOpportunityObserver.cs            # NUEVO — puerto Observer
│   └── Exceptions/
│       └── InvalidOpportunityTransitionException.cs  # NUEVO
│
├── CrudService.Application/
│   └── UseCases/Waitlist/AssignOpportunity/
│       ├── AssignOpportunityCommand.cs        # NUEVO — record(TicketId, EventId)
│       ├── IAssignOpportunityUseCase.cs       # NUEVO — puerto de entrada
│       └── AssignOpportunityHandler.cs        # NUEVO — orquestación completa
│
├── CrudService.Infrastructure/
│   ├── Messaging/
│   │   ├── TicketReleasedConsumer.cs           # NUEVO — consumer RabbitMQ
│   │   ├── TicketReleasedEvent.cs              # NUEVO — DTO del mensaje entrante
│   │   ├── OpportunityActivatedEvent.cs        # NUEVO — DTO del mensaje publicado
│   │   └── RabbitMqPublisher.cs                # NUEVO — publisher genérico (o reutilizar patrón existente)
│   ├── Persistence/Repositories/
│   │   ├── WaitlistOpportunityRepository.cs   # Ya existe — extender
│   │   └── WaitlistEntryRepository.cs         # Ya existe — extender
│   ├── Strategies/
│   │   └── FifoStrategy.cs                    # NUEVO — adaptador IPrioritizationStrategy
│   ├── Services/
│   │   └── TicketReservationAdapter.cs        # NUEVO — adaptador ITicketReservationPort
│   ├── DependencyInjection.cs                 # Ya existe — extender
│   └── TicketingDbContext.cs                  # Ya existe — agregar índice parcial para oportunidad activa
│
└── CrudService.Api/
    └── Program.cs                             # Ya existe — registrar nuevo consumer

crud_service/tests/
├── CrudService.Application.Tests/
│   └── Waitlist/
│       └── AssignOpportunityHandlerTests.cs   # NUEVO — tests unitarios (NSubstitute)
└── CrudService.Infrastructure.Tests/
    └── Integration/
        └── FifoStrategyTests.cs               # NUEVO — tests integración (Testcontainers)
```

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
