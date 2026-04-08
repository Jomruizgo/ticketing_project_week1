# Instrucciones de Copilot — TicketRush MVP

## Idioma y estilo con el usuario
- Responde siempre en español al usuario (salvo que pida explícitamente otro idioma).
- Mantén el código en inglés.
- Mantén la documentación en español, excepto si el usuario solicita otro idioma.
- Mantén un tono crítico y técnico: prioriza precisión y criterio experto por encima de complacencia.
- Si el usuario pide commits, no agregues firmas automáticas tipo "Generated with ..." ni co-author trailers automáticos.

## Flujo Git (obligatorio)
- Este proyecto se trabaja sobre **fork**: usar siempre `fork` como remoto de push y de referencia para PRs.
- Aplicar **GitFlow**: las ramas de feature salen de `develop` y los PR se abren hacia `develop` del fork (no hacia `main` ni hacia upstream salvo instrucción explícita).
- Antes de crear PR, verificar explícitamente: `remote`, `head` y `base` correctos.
- Si hay duda entre upstream/fork, priorizar fork y pedir confirmación solo si el usuario cambia esa regla.

## TDD estricto (obligatorio)
- Cuando el usuario pida TDD, seguir SIEMPRE el orden `RED -> GREEN -> REFACTOR` sin excepciones.
- Regla no negociable: **no modificar código de producción sin una prueba nueva en rojo que justifique el cambio**.
- En cada iteración reportar explícitamente en qué fase está: RED, GREEN o REFACTOR.
- Bloqueos obligatorios:
	- Si no hay evidencia RED, no se implementa.
	- Si GREEN no pasa, no se refactoriza.
	- Si el refactor rompe pruebas, se corrige o revierte antes de continuar.
- Convención de commits para trazabilidad TDD:
	- `test(red): ...`
	- `feat(green): ...`
	- `refactor: ...`
- Si el entorno no permite ejecutar tests (por ejemplo, falta SDK), mantener la secuencia lógica TDD y dejar explícito que la ejecución real debe adjuntarse antes de merge.

## Patrones de diseño (alcance estricto)
- Cuando se hable de **patrones de diseño**, referirse únicamente a la clasificación GoF y a los patrones listados abajo.
- No clasificar DI/IoC/Composition Root como patrón de diseño GoF.

### 1) Patrones Creacionales
- Abstract Factory
- Builder
- Factory Method
- Prototype
- Singleton

### 2) Patrones Estructurales
- Adapter
- Bridge
- Composite
- Decorator
- Facade
- Flyweight
- Proxy

### 3) Patrones de Comportamiento
- Chain of Responsibility
- Command
- Interpreter
- Iterator
- Mediator
- Memento
- Observer
- State
- Strategy
- Template Method
- Visitor

## Visión general del sistema (leer primero)
- El repositorio es un sistema distribuido de venta de entradas: `frontend` (Next.js) + `producer` (publicador HTTP → RabbitMQ) + worker `ReservationService` + worker `paymentService` + `crud_service` (API de lectura/escritura) + PostgreSQL + RabbitMQ.
- El `producer` retorna `202 Accepted` para peticiones de reserva y pago; los cambios de estado ocurren de forma asíncrona en los workers y se leen desde `crud_service`.
- La topología de RabbitMQ está centralizada en `scripts/setup-rabbitmq.sh` (exchange `tickets`, routing keys `ticket.reserved`, `ticket.payment.requested`, `ticket.payments.approved`, `ticket.payments.rejected`). No duplicar declaraciones de colas/exchange en los workers salvo necesidad explícita.
- `compose.yml` es la fuente de verdad operativa: cableado de servicios, variables de entorno, puertos y orden de arranque (`rabbitmq-setup` debe completar antes que producer y consumidores).

## Límites clave del código
- `producer/src/Producer.Api/Controllers/TicketsController.cs` mapea peticiones HTTP de reserva al caso de uso `ReserveTicket`.
- `producer/src/Producer.Api/Controllers/PaymentsController.cs` mapea peticiones HTTP de pago a la publicación del evento `ticket.payment.requested`.
- `crud_service/Controllers/*.cs` es la superficie de API síncrona que usa el frontend para leer eventos, tickets y estados.
- `ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs` aplica bloqueo optimista (`version`) al reservar.
- `paymentService/MsPaymentService.Worker/Services/PaymentValidationService.cs` + `TicketStateService.cs` gestionan idempotencia, TTL y transiciones de estado transaccionales.
- `frontend/lib/api.ts` es el cliente canónico de integración del frontend (URLs base, manejo de errores, semántica asíncrona).

## Convenciones específicas del proyecto (importantes)
- Los estados de ticket/pago en BD son enums PostgreSQL en minúsculas (ver `scripts/schema.sql`), pero los enums de C# pueden ser PascalCase en `ReservationService` y minúsculas en `paymentService`. Respetar cada bounded context; no normalizar a ciegas.
- El flujo de reserva depende de comprobaciones de concurrencia (`WHERE ... version = currentVersion AND status = available`). Preservar este patrón al modificar la lógica de reservas.
- El comportamiento ACK/NACK del worker de pagos es intencional: los fallos de validación de negocio se ACKean; los fallos técnicos se NACKean con `requeue: false` (ver `TicketPaymentConsumer`).
- Producer y CRUD usan CORS permisivo (`AllowAll`) en `Program.cs` para MVP/desarrollo local; no ajustar salvo instrucción explícita.
- Ante conflictos entre código y documentación, el código manda: todos los servicios siguen arquitectura hexagonal con DDD (`Domain/Application/Infrastructure/Worker|Api`). `Domain` define puertos (interfaces); `Infrastructure` implementa adaptadores (EF Core, RabbitMQ, servicios externos). La regla de dependencia es estricta: `Domain` no referencia a nadie.

## Comandos de desarrollo (usar estos primero)
- Infraestructura y servicios completos: `docker compose up -d --build` desde la raíz del repo.
- Frontend en desarrollo: `cd frontend && npm install && npm run dev`.
- Producer en local: `dotnet run --project producer/src/Producer.Api/Producer.Api.csproj`.
- CRUD en local: `dotnet run --project crud_service`.
- Worker de reservas en local: `dotnet run --project ReservationService/src/ReservationService.Worker`.
- Worker de pagos en local: `dotnet run --project paymentService/MsPaymentService.Worker`.
- Tests: `dotnet test paymentService/MsPaymentService.Worker.Tests` y `dotnet test ReservationService/ReservationService.sln`.
- Logs útiles: `docker compose logs -f producer crud-service reservation-service payment rabbitmq`.

## Notas de integración al modificar el sistema
- Si se cambian contratos de API, actualizar tanto el DTO/controller de backend como `frontend/lib/types.ts` y los llamadores de `frontend/lib/api.ts`.
- Mantener los supuestos de UX asíncrona: el frontend espera consistencia eventual mediante hooks de polling (`frontend/hooks/use-reservation-status.ts` y `frontend/hooks/use-payment-status.ts`).
- Si se añaden nuevos eventos RabbitMQ, actualizar: publicador del producer, bindings/colas en `scripts/setup-rabbitmq.sh` y el handler del consumidor correspondiente.
- Para cambios en el modelo de BD, actualizar `scripts/schema.sql` y todos los mappings EF afectados (especialmente conversiones de enums y nombres de columna en snake_case).

## Active Technologies
- C# / .NET 8 + EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1 (001-waitlist-enrollment)
- PostgreSQL (tabla `waitlist_entries`, snake_case automático) (001-waitlist-enrollment)
- PostgreSQL — tablas `waitlist_entries` (existente), `waitlist_opportunities` (futura; se define el modelo pero puede no existir en BD) (002-waitlist-status-query)
- C# / .NET 8 + EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1, RabbitMQ.Client (003-waitlist-opportunity-assignment)
- PostgreSQL (tabla `waitlist_opportunities` existente, columnas snake_case) (003-waitlist-opportunity-assignment)
- C# / .NET 8 + EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, RabbitMQ.Client (004-inapp-notification)
- N/A — esta feature no persiste datos. Consume entidades existentes de solo lectura. (004-inapp-notification)

## Recent Changes
- 001-waitlist-enrollment: Added C# / .NET 8 + EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1
