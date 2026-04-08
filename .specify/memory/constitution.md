<!--
Sync Impact Report
- Version change: N/A → 1.0.0 (initial constitution)
- Modified principles: N/A (initial creation)
- Added sections:
  - Core Principles (8 principios)
  - Development Workflow
  - Quality Gates & CI
  - Governance
- Removed sections: N/A
- Templates requiring updates:
  - .specify/templates/plan-template.md ✅ compatible (Constitution Check section present)
  - .specify/templates/spec-template.md ✅ compatible (no conflicts)
  - .specify/templates/tasks-template.md ✅ compatible (phase structure aligns with TDD)
- Follow-up TODOs: ninguno
-->

# TicketRush Constitution

## Core Principles

### I. Hexagonal Architecture with DDD

Cada microservicio DEBE seguir arquitectura hexagonal con cuatro capas:

- **Domain**: entidades, value objects y puertos (interfaces). NO referencia
  ninguna otra capa ni framework externo.
- **Application**: casos de uso modelados como Command + Handler. Solo
  referencia Domain.
- **Infrastructure**: adaptadores que implementan los puertos de Domain
  (EF Core para PostgreSQL, publishers/consumers de RabbitMQ, servicios
  externos).
- **Api/Worker**: punto de entrada. Orquesta composición de dependencias;
  NO contiene lógica de negocio.

La regla de dependencia es estricta e inviolable: ningún framework,
ORM ni librería de mensajería DEBE filtrarse hacia Domain o Application.

### II. Asynchronous-First Pipeline

El flujo principal de negocio DEBE ser asíncrono:

- El Producer recibe peticiones HTTP y retorna `202 Accepted`; publica
  eventos en RabbitMQ (exchange `tickets`).
- Los Workers (ReservationService, PaymentService) consumen eventos y
  procesan cambios de estado de forma independiente.
- El CRUD Service gestiona lecturas/escrituras síncronas y notificaciones
  SSE para consistencia eventual en el frontend.
- Los cambios de estado NUNCA son síncronos desde la perspectiva del
  cliente HTTP.

### III. TDD Strict (NON-NEGOTIABLE)

El ciclo RED → GREEN → REFACTOR es obligatorio sin excepciones:

- **RED**: DEBE existir una prueba nueva que falle antes de escribir
  cualquier código de producción.
- **GREEN**: implementar el mínimo código necesario para que la prueba
  pase.
- **REFACTOR**: mejorar estructura solo si todas las pruebas están en
  verde. Si el refactor rompe pruebas, se corrige o revierte antes de
  continuar.

Convención de commits para trazabilidad TDD:
- `test(red): <descripción de la prueba fallida>`
- `feat(green): <implementación mínima>`
- `refactor: <mejora de estructura>`

### IV. Testing Pyramid

La estrategia de pruebas DEBE respetar la pirámide estricta:

- **Unitarias** (base): mocks/stubs para lógica de Domain y Application.
  Máxima cobertura, ejecución rápida, sin dependencias externas.
- **Integración** (medio): Testcontainers para PostgreSQL con teardown
  adecuado mediante `IAsyncLifetime`. Verifican adaptadores de
  Infrastructure contra recursos reales.
- **E2E / Aceptación** (cima): sobre Docker Compose. Validan flujos
  completos del sistema distribuido.

### V. SOLID (NON-NEGOTIABLE)

Los cinco principios SOLID son innegociables:

- **S** — Cada clase o método DEBE tener una única razón para cambiar.
- **O** — Las entidades DEBEN estar abiertas a extensión, cerradas a
  modificación.
- **L** — Los subtipos DEBEN ser sustituibles por sus tipos base sin
  alterar el comportamiento.
- **I** — Los clientes NO DEBEN depender de interfaces que no usan.
- **D** — Los módulos de alto nivel NO DEBEN depender de módulos de bajo
  nivel; ambos DEBEN depender de abstracciones.

### VI. GoF-Only Design Patterns

Los patrones de diseño DEBEN referirse exclusivamente a la clasificación
GoF (Gang of Four): Creacionales (Abstract Factory, Builder,
Factory Method, Prototype, Singleton), Estructurales (Adapter, Bridge,
Composite, Decorator, Facade, Flyweight, Proxy) y de Comportamiento
(Chain of Responsibility, Command, Interpreter, Iterator, Mediator,
Memento, Observer, State, Strategy, Template Method, Visitor).

DI/IoC/Composition Root NO se clasifican como patrón de diseño GoF.

### VII. Code Quality: Zero AI Smells

El código DEBE cumplir estos estándares sin excepción:

- Sin nombres genéricos (`data`, `result`, `info`, `temp`, `obj`).
  Toda variable, clase y método DEBE tener nomenclatura intencional y
  descriptiva.
- Sin comentarios que expliquen sintaxis obvia.
- Sin imports/usings muertos.
- Sin bloques `try-catch` vacíos o que traguen excepciones.
- Sin duplicación de código; extraer abstracciones solo cuando la
  duplicación ya existe, no preventivamente.
- Sin código muerto ni clases/métodos sin uso.

### VIII. Language Convention

- El código DEBE estar en inglés (nombres de clases, métodos, variables,
  mensajes de error técnicos).
- La documentación DEBE estar en español.
- Los commits DEBEN estar en inglés siguiendo Conventional Commits.

## Development Workflow

El proyecto sigue **GitFlow** con fork como remoto de referencia:

- Las ramas de feature salen de `develop`.
- Los PRs se abren hacia `develop` del fork (nunca hacia `main` ni
  upstream salvo instrucción explícita).
- Antes de crear un PR se DEBE verificar: `remote`, `head` y `base`
  correctos.
- Cada PR DEBE pasar CI antes de ser elegible para merge.

Stack tecnológico:
- **Backend**: .NET 8, C#, EF Core, RabbitMQ.Client
- **Frontend**: Next.js, TypeScript, Tailwind CSS
- **Infraestructura**: PostgreSQL, RabbitMQ, Docker Compose
- **CI**: GitHub Actions

## Quality Gates & CI

CI corre en GitHub Actions como **Quality Gate bloqueante**:

- El merge a `develop` está PROHIBIDO si algún check falla.
- Checks obligatorios:
  - Compilación exitosa de todos los servicios.
  - Ejecución de pruebas unitarias y de integración.
  - Análisis estático / linting sin errores.
- Cada PR DEBE ser revisado y aprobado antes del merge.
- La evidencia de ejecución TDD (RED → GREEN → REFACTOR) DEBE ser
  adjuntable o trazable en el historial de commits.

## Governance

Esta constitución es el documento rector del proyecto TicketRush.
Todas las decisiones técnicas, revisiones de código y PRs DEBEN
verificar cumplimiento con estos principios.

- Las enmiendas REQUIEREN documentación explícita del cambio, justificación
  técnica y actualización de la versión.
- El versionado sigue SemVer: MAJOR para cambios incompatibles en
  principios, MINOR para adiciones, PATCH para clarificaciones.
- Ante conflictos entre documentación y código, el código manda.
- Usar `.github/copilot-instructions.md` como guía de desarrollo en
  runtime para asistentes de IA.
- Toda complejidad añadida DEBE justificarse frente a estos principios.

**Version**: 1.0.0 | **Ratified**: 2026-04-07 | **Last Amended**: 2026-04-07
