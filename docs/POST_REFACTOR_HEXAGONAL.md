# Post-refactorización: validación técnica (todos los microservicios)

## Alcance

Este documento evidencia la calidad de la refactorización en los cuatro microservicios migrados a arquitectura hexagonal:

- ReservationService
- Producer
- CrudService
- PaymentService

---

## 1) ReservationService

### 1.1 Evidencia de arquitectura hexagonal y separación de capas

Estructura por capas (compilation-enforced):

- Domain: [ReservationService/src/ReservationService.Domain/ReservationService.Domain.csproj](ReservationService/src/ReservationService.Domain/ReservationService.Domain.csproj)
- Application: [ReservationService/src/ReservationService.Application/ReservationService.Application.csproj](ReservationService/src/ReservationService.Application/ReservationService.Application.csproj)
- Infrastructure: [ReservationService/src/ReservationService.Infrastructure/ReservationService.Infrastructure.csproj](ReservationService/src/ReservationService.Infrastructure/ReservationService.Infrastructure.csproj)
- Worker (composition root): [ReservationService/src/ReservationService.Worker/ReservationService.Worker.csproj](ReservationService/src/ReservationService.Worker/ReservationService.Worker.csproj)

Regla de dependencias aplicada:

- `Domain` no referencia proyectos internos ni infraestructura.
- `Application` referencia solo `Domain`.
- `Infrastructure` referencia `Domain + Application`.
- `Worker` referencia `Infrastructure`.

Puertos definidos y usados por casos de uso:

- Puerto de salida de dominio: [ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs](ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs)
- Caso de uso desacoplado de infraestructura: [ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs](ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs)

### 1.2 Evidencia SOLID (definición + ejemplo + aplicación real)

#### S: Single Responsibility Principle (Responsabilidad Única)
**Definición:** "Una clase debe tener un solo motivo de cambio".

**Ejemplo clave:**
Si una clase `FacturaService` calcula impuestos, guarda en base de datos y además envía correos, cualquier cambio fiscal, de persistencia o de notificación obliga a tocar la misma clase. Eso incrementa acoplamiento y riesgo.

**Cómo cumple nuestra implementación:**
- [ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs](ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs) solo orquesta reglas de negocio de reserva.
- [ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs](ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs) solo gestiona persistencia/concurrencia.
- [ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs](ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs) solo resuelve entrada/salida de mensajería.

#### O: Open/Closed Principle (Abierto/Cerrado)
**Definición:** "Las entidades deben estar abiertas para extensión, cerradas para modificación".

**Ejemplo clave:**
Si `PaymentCalculator` tiene `if` por tipo de pago y cada nuevo método obliga a editar esa clase, se rompe OCP. Mejor extender mediante nuevas implementaciones de un contrato común.

**Cómo cumple nuestra implementación:**
- El caso de uso depende del puerto [ITicketRepository](ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs).
- Se puede agregar otro adaptador (por ejemplo otro motor de persistencia) sin modificar el handler de aplicación.

#### L: Liskov Substitution Principle (Sustitución de Liskov)
**Definición:** "Los subtipos deben ser sustituibles por su tipo base sin romper el programa".

**Ejemplo clave:**
Si `Ave` define `Volar()` y `Pingüino : Ave` no puede volar, hay una jerarquía mal modelada. Sustituir `Pingüino` por `Ave` rompe expectativas del cliente.

**Regla práctica:**
La sustitución puede ser por herencia **o por implementación de interfaz**; lo importante no es la sintaxis, es respetar el contrato comportamental.

**Cómo cumple nuestra implementación:**
- `ProcessReservationCommandHandler` trabaja contra [ITicketRepository](ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs), no contra una clase concreta.
- Cualquier implementación que respete el contrato (mismas pre/post-condiciones de `GetByIdAsync` y `TryReserveAsync`) puede sustituirse sin romper el caso de uso.

#### I: Interface Segregation Principle (Segregación de Interfaces)
**Definición:** "Ningún cliente debe depender de métodos que no usa".

**Ejemplo clave:**
Una interfaz `IRepository` con 20 métodos obliga a servicios simples a depender de operaciones irrelevantes. Eso añade ruido, acoplamiento y mocks innecesarios.

**Cómo cumple nuestra implementación:**
- [ITicketRepository](ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs) expone operaciones puntuales para el caso de uso de reserva.
- El handler mockea únicamente las operaciones que consume.

#### D: Dependency Inversion Principle (Inversión de Dependencias)
**Definición:** "Los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones".

**Ejemplo clave:**
Si `OrderService` instancia directamente `SqlConnection`, la lógica queda atada a un detalle técnico y es difícil testearla.

**Cómo cumple nuestra implementación:**
- Application depende de [ITicketRepository](ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs) (abstracción del dominio).
- Infrastructure implementa ese puerto con EF/Npgsql en [TicketRepository](ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs).

### 1.3 Patrones de diseño aplicados (definición + ejemplo + aplicación real)

#### Patrón arquitectónico: Ports & Adapters (Hexagonal)
**Definición:** El dominio y los casos de uso se diseñan como núcleo aislado; todo acceso a infraestructura ocurre a través de puertos.

**Ejemplo clave:**
Si la lógica de reserva dependiera directamente de RabbitMQ o EF Core, cada cambio técnico obligaría a tocar negocio.

**Cómo cumple nuestra implementación:**
- `Application` procesa la reserva sin conocer detalles de broker o base de datos.
- `Infrastructure` conecta ese núcleo mediante adaptadores concretos:
  - [ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs](ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs)
  - [ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs](ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs)

#### Patrón creacional: Dependency Injection (Composition Root)
**Definición:** La creación y ensamblaje de dependencias se centraliza en un punto de composición, fuera de la lógica de negocio.

**Ejemplo clave:**
Sin DI, un handler podría instanciar su repositorio y su logger; eso acopla construcción y comportamiento.

**Cómo cumple nuestra implementación:**
- El registro de servicios está centralizado en [ReservationService/src/ReservationService.Infrastructure/DependencyInjection.cs](ReservationService/src/ReservationService.Infrastructure/DependencyInjection.cs).
- Los casos de uso reciben dependencias por constructor, lo que facilita sustitución en tests.

#### Patrón estructural: Repository
**Definición:** Se encapsula el acceso a persistencia detrás de una abstracción orientada al dominio.

**Ejemplo clave:**
La regla de concurrencia optimista (`version` + `status`) no debería dispersarse en handlers y consumers.

**Cómo cumple nuestra implementación:**
- `ITicketRepository` define operaciones del dominio.
- `TicketRepository` concentra SQL/EF y semántica de concurrencia en [ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs](ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs).

#### Patrón de comportamiento: Command Handler + Event-Driven Consumer
**Definición:** Un command handler encapsula el caso de uso; un consumidor de eventos actúa como disparador asíncrono.

**Ejemplo clave:**
Si el consumer mezclara parsing del mensaje, reglas de reserva y persistencia en un solo bloque, crecería la complejidad y bajaría la testabilidad.

**Cómo cumple nuestra implementación:**
- El flujo de negocio se ejecuta en [ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs](ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs).
- RabbitMQ sólo activa el caso de uso y publica resultado en [ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs](ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs).

### 1.4 Tests unitarios puros y cobertura lógica

Suite principal de aplicación:

- [ReservationService/tests/ReservationService.Application.Tests/ProcessReservationCommandHandlerTests.cs](ReservationService/tests/ReservationService.Application.Tests/ProcessReservationCommandHandlerTests.cs)

Evidencia de pureza unitaria:

- Uso de mocks/substitutes sobre puerto de salida (`ITicketRepository`).
- Sin DB real, sin RabbitMQ real, sin Docker para la lógica del caso de uso.

Cobertura lógica del caso de uso `ProcessReservation`:

- Ticket no existe.
- Ticket no disponible.
- Reserva exitosa.
- Conflicto de concurrencia (modificación concurrente).

### 1.5 HUMAN CHECKs aplicados

Se marcaron decisiones críticas en código:

- Optimistic locking (filtro por `version` + `status`) en [ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs](ReservationService/src/ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs).
- Orden de publicación `status.changed` después del procesamiento de reserva en [ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs](ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs).

---

## 2) Producer

### 2.1 Evidencia de arquitectura hexagonal y separación de capas

Estructura por capas:

- Domain: [producer/src/Producer.Domain/Producer.Domain.csproj](producer/src/Producer.Domain/Producer.Domain.csproj)
- Application: [producer/src/Producer.Application/Producer.Application.csproj](producer/src/Producer.Application/Producer.Application.csproj)
- Infrastructure: [producer/src/Producer.Infrastructure/Producer.Infrastructure.csproj](producer/src/Producer.Infrastructure/Producer.Infrastructure.csproj)
- Api (composition root): [producer/src/Producer.Api/Producer.Api.csproj](producer/src/Producer.Api/Producer.Api.csproj)

Regla de dependencias aplicada:

- `Producer.Domain` no importa infraestructura.
- `Producer.Application` depende solo de `Producer.Domain`.
- `Producer.Infrastructure` implementa puertos de dominio.
- `Producer.Api` orquesta DI y expone HTTP.

Puertos de salida del dominio bien definidos:

- [producer/src/Producer.Domain/Ports/ITicketEventPublisher.cs](producer/src/Producer.Domain/Ports/ITicketEventPublisher.cs)
- [producer/src/Producer.Domain/Ports/IPaymentEventPublisher.cs](producer/src/Producer.Domain/Ports/IPaymentEventPublisher.cs)

Casos de uso desacoplados:

- Reserva: [producer/src/Producer.Application/UseCases/ReserveTicket/ReserveTicketCommandHandler.cs](producer/src/Producer.Application/UseCases/ReserveTicket/ReserveTicketCommandHandler.cs)
- Pago: [producer/src/Producer.Application/UseCases/RequestPayment/RequestPaymentCommandHandler.cs](producer/src/Producer.Application/UseCases/RequestPayment/RequestPaymentCommandHandler.cs)

### 2.2 Evidencia SOLID (definición + ejemplo + aplicación real)

#### S: Single Responsibility Principle (Responsabilidad Única)
**Definición:** "Una clase debe tener un solo motivo de cambio".

**Ejemplo clave:**
Si un controller también decide reglas de negocio y además persiste, cualquier cambio en validación, reglas o infraestructura impacta el mismo archivo.

**Cómo cumple nuestra implementación:**
- Controllers HTTP en [producer/src/Producer.Api/Controllers](producer/src/Producer.Api/Controllers) validan y mapean request.
- Casos de uso en [producer/src/Producer.Application/UseCases](producer/src/Producer.Application/UseCases) construyen eventos y ejecutan el flujo de negocio.
- Adaptadores RabbitMQ en [producer/src/Producer.Infrastructure/Messaging](producer/src/Producer.Infrastructure/Messaging) encapsulan publicación.

#### O: Open/Closed Principle (Abierto/Cerrado)
**Definición:** "Extender sin modificar lógica estable".

**Ejemplo clave:**
Si para cada broker nuevo hay que editar handlers de negocio, la capa de aplicación está mal cerrada.

**Cómo cumple nuestra implementación:**
- Handlers dependen de puertos [ITicketEventPublisher](producer/src/Producer.Domain/Ports/ITicketEventPublisher.cs) e [IPaymentEventPublisher](producer/src/Producer.Domain/Ports/IPaymentEventPublisher.cs).
- Se puede extender con nuevos adaptadores de publicación sin tocar `Application`.

#### L: Liskov Substitution Principle (Sustitución de Liskov)
**Definición:** "Un subtipo debe poder reemplazar a su tipo base sin romper comportamiento esperado".

**Ejemplo clave:**
Una implementación de `INotifier` que "simule éxito" sin notificar realmente viola LSP si el cliente confía en la semántica de confirmación.

**Cómo cumple nuestra implementación:**
- `RequestPaymentCommandHandler` y `ReserveTicketCommandHandler` consumen puertos del dominio, no clases concretas.
- Cualquier implementación de esos puertos que respete el contrato semántico (publicar evento correcto o fallar de forma consistente) es sustituible para los handlers.

#### I: Interface Segregation Principle (Segregación de Interfaces)
**Definición:** "Interfaces pequeñas y enfocadas por cliente".

**Ejemplo clave:**
Una interfaz única `IEventPublisher` con métodos de tickets, pagos, auditoría y expiración obliga dependencias innecesarias.

**Cómo cumple nuestra implementación:**
- Puerto de ticket separado de puerto de pago:
  - [ITicketEventPublisher](producer/src/Producer.Domain/Ports/ITicketEventPublisher.cs)
  - [IPaymentEventPublisher](producer/src/Producer.Domain/Ports/IPaymentEventPublisher.cs)

#### D: Dependency Inversion Principle (Inversión de Dependencias)
**Definición:** "La política de negocio no depende de detalles técnicos".

**Ejemplo clave:**
Si el caso de uso instancia `IConnection` de RabbitMQ, queda acoplado a infraestructura y pierde testabilidad.

**Cómo cumple nuestra implementación:**
- `Producer.Application` depende solo de puertos en `Producer.Domain`.
- `Producer.Infrastructure` contiene RabbitMQ, DI y configuración concreta.

### 2.3 Patrones de diseño aplicados (definición + ejemplo + aplicación real)

#### Patrón arquitectónico: Ports & Adapters (Hexagonal)
**Definición:** Se separa el núcleo de negocio de los detalles de entrada/salida mediante puertos y adaptadores.

**Ejemplo clave:**
Si `ReserveTicketCommandHandler` conociera exchange, routing key y serialización, cualquier cambio de mensajería rompería Application.

**Cómo cumple nuestra implementación:**
- Los handlers dependen de puertos de dominio:
  - [producer/src/Producer.Domain/Ports/ITicketEventPublisher.cs](producer/src/Producer.Domain/Ports/ITicketEventPublisher.cs)
  - [producer/src/Producer.Domain/Ports/IPaymentEventPublisher.cs](producer/src/Producer.Domain/Ports/IPaymentEventPublisher.cs)
- Los detalles RabbitMQ viven en Infrastructure.

#### Patrón creacional: Dependency Injection + Factory
**Definición:** DI resuelve dependencias por contrato; Factory encapsula construcción compleja de objetos técnicos.

**Ejemplo clave:**
Si cada publisher creara su propia conexión con parámetros hardcodeados, habría duplicación y riesgo de configuración inconsistente.

**Cómo cumple nuestra implementación:**
- El wiring se centraliza en [producer/src/Producer.Infrastructure/DependencyInjection.cs](producer/src/Producer.Infrastructure/DependencyInjection.cs).
- `ConnectionFactory` de RabbitMQ encapsula creación de conexiones/canales según configuración.

#### Patrón estructural: Adapter
**Definición:** Un adapter traduce una interfaz esperada por negocio a una API externa concreta.

**Ejemplo clave:**
Application necesita "publicar evento"; RabbitMQ exige exchange/routing key/body/propiedades. El adapter convierte entre ambos mundos.

**Cómo cumple nuestra implementación:**
- [producer/src/Producer.Infrastructure/Messaging/RabbitMQTicketPublisher.cs](producer/src/Producer.Infrastructure/Messaging/RabbitMQTicketPublisher.cs) adapta `ITicketEventPublisher`.
- [producer/src/Producer.Infrastructure/Messaging/RabbitMQPaymentPublisher.cs](producer/src/Producer.Infrastructure/Messaging/RabbitMQPaymentPublisher.cs) adapta `IPaymentEventPublisher`.

#### Patrón de comportamiento: Command Handler
**Definición:** Cada comando tiene un handler que encapsula una intención de negocio única y su flujo.

**Ejemplo clave:**
Separar `ReserveTicket` y `RequestPayment` evita handlers genéricos con condicionales por tipo de operación.

**Cómo cumple nuestra implementación:**
- Reserva en [producer/src/Producer.Application/UseCases/ReserveTicket/ReserveTicketCommandHandler.cs](producer/src/Producer.Application/UseCases/ReserveTicket/ReserveTicketCommandHandler.cs).
- Pago en [producer/src/Producer.Application/UseCases/RequestPayment/RequestPaymentCommandHandler.cs](producer/src/Producer.Application/UseCases/RequestPayment/RequestPaymentCommandHandler.cs).

### 2.4 Tests unitarios puros y cobertura lógica

Suite:

- [producer/tests/Producer.Application.Tests/UseCases/ReserveTicket/ReserveTicketCommandHandlerTests.cs](producer/tests/Producer.Application.Tests/UseCases/ReserveTicket/ReserveTicketCommandHandlerTests.cs)
- [producer/tests/Producer.Application.Tests/UseCases/RequestPayment/RequestPaymentCommandHandlerTests.cs](producer/tests/Producer.Application.Tests/UseCases/RequestPayment/RequestPaymentCommandHandlerTests.cs)

Evidencia de pureza unitaria:

- Mocks (`NSubstitute`) sobre puertos de salida.
- Sin RabbitMQ real ni dependencias de infraestructura en tests.

Cobertura lógica de `Application` en Producer:

- Mapeo correcto `ReserveTicketCommand -> TicketReservedEvent`.
- Mapeo correcto `RequestPaymentCommand -> PaymentRequestedEvent`.
- Generación de `TransactionRef` cuando viene `null`.

### 2.5 HUMAN CHECKs aplicados

- Política CORS de desarrollo y advertencia de hardening en [producer/src/Producer.Api/Program.cs](producer/src/Producer.Api/Program.cs).
- Gestión de secretos y configuración de conexión RabbitMQ en [producer/src/Producer.Infrastructure/DependencyInjection.cs](producer/src/Producer.Infrastructure/DependencyInjection.cs).

---

## 3) CrudService

### 3.1 Evidencia de arquitectura hexagonal y separación de capas

Estructura por capas (compilation-enforced):

- Domain: [crud_service/src/CrudService.Domain/CrudService.Domain.csproj](crud_service/src/CrudService.Domain/CrudService.Domain.csproj)
- Application: [crud_service/src/CrudService.Application/CrudService.Application.csproj](crud_service/src/CrudService.Application/CrudService.Application.csproj)
- Infrastructure: [crud_service/src/CrudService.Infrastructure/CrudService.Infrastructure.csproj](crud_service/src/CrudService.Infrastructure/CrudService.Infrastructure.csproj)
- Api (composition root): [crud_service/src/CrudService.Api/CrudService.Api.csproj](crud_service/src/CrudService.Api/CrudService.Api.csproj)

Regla de dependencias aplicada:

- `CrudService.Domain` no referencia proyectos internos ni infraestructura.
- `CrudService.Application` referencia solo `Domain`.
- `CrudService.Infrastructure` referencia `Domain + Application`.
- `CrudService.Api` referencia `Infrastructure` (transitivo a todo).

Puertos definidos y usados por casos de uso:

- Puerto de eventos: [crud_service/src/CrudService.Domain/Interfaces/IEventRepository.cs](crud_service/src/CrudService.Domain/Interfaces/IEventRepository.cs)
- Puerto de tickets: [crud_service/src/CrudService.Domain/Interfaces/ITicketRepository.cs](crud_service/src/CrudService.Domain/Interfaces/ITicketRepository.cs)
- Puerto de historial: [crud_service/src/CrudService.Domain/Interfaces/ITicketHistoryRepository.cs](crud_service/src/CrudService.Domain/Interfaces/ITicketHistoryRepository.cs)

Casos de uso desacoplados de infraestructura:

- Events (5): [crud_service/src/CrudService.Application/UseCases/Events/](crud_service/src/CrudService.Application/UseCases/Events/)
- Tickets (6): [crud_service/src/CrudService.Application/UseCases/Tickets/](crud_service/src/CrudService.Application/UseCases/Tickets/)

### 3.2 Evidencia SOLID (definición + ejemplo + aplicación real)

#### S: Single Responsibility Principle (Responsabilidad Única)
**Definición:** "Una clase debe tener un solo motivo de cambio".

**Ejemplo clave:**
Si un único `EventService` maneja `GetAll`, `GetById`, `Create`, `Update` y `Delete`, cualquier cambio en la lógica de creación (nueva validación, nuevo campo) obliga a abrir un archivo que también contiene lógica de lectura. El riesgo de efectos secundarios aumenta con cada responsabilidad acumulada.

**Cómo cumple nuestra implementación:**
- [CreateEventCommandHandler](crud_service/src/CrudService.Application/UseCases/Events/CreateEvent/CreateEventCommandHandler.cs) — solo valida y persiste un evento nuevo.
- [UpdateTicketStatusCommandHandler](crud_service/src/CrudService.Application/UseCases/Tickets/UpdateTicketStatus/UpdateTicketStatusCommandHandler.cs) — solo valida la transición de estado y registra historial.
- [GetAllEventsQueryHandler](crud_service/src/CrudService.Application/UseCases/Events/GetAllEvents/GetAllEventsQueryHandler.cs) — solo recupera y mapea la lista de eventos.
- Los 11 handlers tienen exactamente un motivo de cambio cada uno.

#### O: Open/Closed Principle (Abierto/Cerrado)
**Definición:** "Las entidades deben estar abiertas para extensión, cerradas para modificación".

**Ejemplo clave:**
Si agregar un caso de uso nuevo (por ejemplo `GetEventsByDate`) obliga a modificar un `EventService` existente, ese servicio no está cerrado. El riesgo es introducir regresiones en operaciones que ya funcionan.

**Cómo cumple nuestra implementación:**
- Agregar un nuevo caso de uso implica crear un nuevo handler en `Application/UseCases/` e inyectarlo en el controlador, sin tocar ninguno de los 11 handlers existentes.
- Los controladores de [CrudService.Api/Controllers](crud_service/src/CrudService.Api/Controllers/) inyectan cada handler directamente; añadir uno nuevo es una extensión pura, no una modificación.

#### L: Liskov Substitution Principle (Sustitución de Liskov)
**Definición:** "Los subtipos deben ser sustituibles por su tipo base sin romper el programa".

**Ejemplo clave:**
Si `ITicketRepository.GetByIdAsync` promete retornar un `Ticket?` y una implementación concreta lanza una excepción no documentada, el handler que depende de la interfaz queda roto sin saberlo.

**Cómo cumple nuestra implementación:**
- `EventRepository`, `TicketRepository` y `TicketHistoryRepository` implementan sus interfaces respetando exactamente el contrato de pre/post-condiciones: `GetByIdAsync` retorna `null` si no existe, `AddAsync` siempre retorna la entidad persistida.
- Los handlers son sustitución-transparentes: en tests, `NSubstitute` reemplaza cada repositorio por un sustituto que respeta el mismo contrato y los handlers funcionan igual.

#### I: Interface Segregation Principle (Segregación de Interfaces)
**Definición:** "Ningún cliente debe depender de métodos que no usa".

**Ejemplo clave:**
Una interfaz `IRepository` con operaciones de eventos, tickets, pagos e historial obliga a `CreateEventCommandHandler` a depender (aunque sea transitivamente) de métodos de tickets que no necesita. Eso incrementa el acoplamiento y genera mocks más complejos.

**Cómo cumple nuestra implementación:**
- `CreateEventCommandHandler` depende exclusivamente de [IEventRepository](crud_service/src/CrudService.Domain/Interfaces/IEventRepository.cs) (6 métodos de eventos).
- `UpdateTicketStatusCommandHandler` depende de [ITicketRepository](crud_service/src/CrudService.Domain/Interfaces/ITicketRepository.cs) (7 métodos de tickets) y [ITicketHistoryRepository](crud_service/src/CrudService.Domain/Interfaces/ITicketHistoryRepository.cs) (3 métodos de historial).
- Ningún handler conoce métodos de otro dominio: 3 interfaces pequeñas y enfocadas.

#### D: Dependency Inversion Principle (Inversión de Dependencias)
**Definición:** "Los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones".

**Ejemplo clave:**
Si `UpdateTicketStatusCommandHandler` instanciara `TicketRepository` (EF Core) directamente, la lógica de negocio quedaría atada a una decisión técnica de persistencia.

**Cómo cumple nuestra implementación:**
- Application depende únicamente de los puertos definidos en Domain (`IEventRepository`, `ITicketRepository`, `ITicketHistoryRepository`).
- Infrastructure implementa esos puertos con EF Core en [crud_service/src/CrudService.Infrastructure/Persistence/Repositories/](crud_service/src/CrudService.Infrastructure/Persistence/Repositories/).
- Cambiar de EF Core a Dapper requeriría únicamente reemplazar las implementaciones en Infrastructure, sin tocar Application.

### 3.3 Patrones de diseño aplicados (definición + ejemplo + aplicación real)

#### Patrón arquitectónico: Ports & Adapters (Hexagonal)
**Definición:** El dominio y los casos de uso conforman el núcleo aislado; toda integración con infraestructura ocurre a través de puertos explícitos.

**Ejemplo clave:**
Si los handlers de Application importaran `DbContext` directamente, cada migración de base de datos o cambio de ORM impactaría la lógica de negocio.

**Cómo cumple nuestra implementación:**
- Los 11 handlers de Application no referencian EF Core, PostgreSQL ni RabbitMQ.
- La capa Infrastructure provee los adaptadores concretos:
  - [EventRepository](crud_service/src/CrudService.Infrastructure/Persistence/Repositories/EventRepository.cs)
  - [TicketRepository](crud_service/src/CrudService.Infrastructure/Persistence/Repositories/TicketRepository.cs)
  - [TicketStatusConsumer](crud_service/src/CrudService.Infrastructure/Messaging/TicketStatusConsumer.cs) (adaptador de entrada RabbitMQ para SSE)

#### Patrón creacional: Dependency Injection (Composition Root)
**Definición:** La construcción y ensamblaje de todas las dependencias se centraliza en un único punto de composición, fuera de la lógica de negocio.

**Ejemplo clave:**
Sin DI, un handler instanciaría su repositorio con `new TicketRepository(new TicketingDbContext(...))`, acoplando construcción y comportamiento.

**Cómo cumple nuestra implementación:**
- Todo el registro de servicios está centralizado en [crud_service/src/CrudService.Infrastructure/DependencyInjection.cs](crud_service/src/CrudService.Infrastructure/DependencyInjection.cs).
- Los 11 handlers y los 3 repositorios se inyectan por constructor; ninguna clase crea sus propias dependencias.

#### Patrón estructural: Repository
**Definición:** Se encapsula el acceso a persistencia detrás de una abstracción orientada al dominio, ocultando los detalles de la base de datos.

**Ejemplo clave:**
Sin Repository, la lógica de cómo mapear un `Event` a columnas de PostgreSQL estaría dispersa en varios handlers.

**Cómo cumple nuestra implementación:**
- Tres interfaces segregadas en Domain definen contratos orientados al negocio.
- Tres implementaciones en Infrastructure encapsulan EF Core, nombre de columnas en snake_case y queries específicas (`GetExpiredAsync`, `CountByStatusAsync`).

#### Patrón de comportamiento: Command/Query Handler (CQRS-lite)
**Definición:** Cada intención de negocio (comando o consulta) tiene un handler dedicado que encapsula exactamente ese flujo, sin dependencias cruzadas entre operaciones.

**Ejemplo clave:**
Un `EventService` monolítico con 5 métodos públicos mezcla todas las intenciones en una sola clase; cualquier cambio en una operación puede afectar las demás.

**Cómo cumple nuestra implementación:**
- 5 handlers de eventos (GetAll, GetById, Create, Update, Delete) y 6 handlers de tickets (GetByEvent, GetById, Create, UpdateStatus, Release, GetExpired): cada uno en su propio directorio con Command/Query + Handler.
- En tests, cada handler se prueba de forma completamente aislada.

### 3.4 Tests unitarios puros y cobertura lógica

Suite principal de aplicación:

- Events: [crud_service/tests/CrudService.Application.Tests/Events/](crud_service/tests/CrudService.Application.Tests/Events/)
- Tickets: [crud_service/tests/CrudService.Application.Tests/Tickets/](crud_service/tests/CrudService.Application.Tests/Tickets/)

Evidencia de pureza unitaria:

- Uso de NSubstitute sobre los tres puertos de salida (`IEventRepository`, `ITicketRepository`, `ITicketHistoryRepository`).
- Sin DB real, sin RabbitMQ real, sin Docker para la lógica de los casos de uso.

Cobertura lógica por caso de uso:

- `GetAllEvents` / `GetEventById`: evento no encontrado; lista vacía; mapeo correcto a DTO.
- `CreateEvent`: creación exitosa con todos los campos; retorno de DTO correcto.
- `UpdateEvent`: actualización parcial (solo campos no nulos); evento no encontrado lanza excepción.
- `DeleteEvent`: eliminación exitosa; evento no encontrado lanza excepción.
- `GetTicketsByEvent` / `GetTicketById`: ticket no encontrado; mapeo correcto a DTO.
- `CreateTickets`: creación de N tickets con estado inicial `Available`; vinculación al evento.
- `UpdateTicketStatus`: transición de estado válida con registro de historial; estado inválido lanza `InvalidTicketStatusException`.
- `ReleaseTicket`: liberación exitosa; ticket no encontrado lanza excepción.
- `GetExpiredTickets`: retorno de tickets expirados con mapeo correcto.

---

## 4) PaymentService

### 4.1 Evidencia de arquitectura hexagonal y separación de capas

Estructura por capas (compilation-enforced):

- Domain: [paymentService/src/MsPaymentService.Domain/MsPaymentService.Domain.csproj](paymentService/src/MsPaymentService.Domain/MsPaymentService.Domain.csproj)
- Application: [paymentService/src/MsPaymentService.Application/MsPaymentService.Application.csproj](paymentService/src/MsPaymentService.Application/MsPaymentService.Application.csproj)
- Infrastructure: [paymentService/src/MsPaymentService.Infrastructure/MsPaymentService.Infrastructure.csproj](paymentService/src/MsPaymentService.Infrastructure/MsPaymentService.Infrastructure.csproj)
- Worker (composition root): [paymentService/src/MsPaymentService.Worker/MsPaymentService.Worker.csproj](paymentService/src/MsPaymentService.Worker/MsPaymentService.Worker.csproj)

Regla de dependencias aplicada:

- `MsPaymentService.Domain` no referencia proyectos internos ni infraestructura.
- `MsPaymentService.Application` referencia solo `Domain`.
- `MsPaymentService.Infrastructure` referencia `Domain + Application`.
- `MsPaymentService.Worker` referencia `Infrastructure`.

Puertos de salida del dominio:

- [paymentService/src/MsPaymentService.Domain/Interfaces/ITicketRepository.cs](paymentService/src/MsPaymentService.Domain/Interfaces/ITicketRepository.cs)
- [paymentService/src/MsPaymentService.Domain/Interfaces/IPaymentRepository.cs](paymentService/src/MsPaymentService.Domain/Interfaces/IPaymentRepository.cs)
- [paymentService/src/MsPaymentService.Domain/Interfaces/ITicketHistoryRepository.cs](paymentService/src/MsPaymentService.Domain/Interfaces/ITicketHistoryRepository.cs)

Puerto de Application (servicio de transiciones de estado):

- [paymentService/src/MsPaymentService.Application/Interfaces/ITicketStateService.cs](paymentService/src/MsPaymentService.Application/Interfaces/ITicketStateService.cs)

Casos de uso desacoplados:

- [paymentService/src/MsPaymentService.Application/UseCases/ProcessApprovedPayment/ProcessApprovedPaymentCommandHandler.cs](paymentService/src/MsPaymentService.Application/UseCases/ProcessApprovedPayment/ProcessApprovedPaymentCommandHandler.cs)
- [paymentService/src/MsPaymentService.Application/UseCases/ProcessRejectedPayment/ProcessRejectedPaymentCommandHandler.cs](paymentService/src/MsPaymentService.Application/UseCases/ProcessRejectedPayment/ProcessRejectedPaymentCommandHandler.cs)

### 4.2 Evidencia SOLID (definición + ejemplo + aplicación real)

#### S: Single Responsibility Principle (Responsabilidad Única)
**Definición:** "Una clase debe tener un solo motivo de cambio".

**Ejemplo clave:**
Si un único `PaymentService` deserializa mensajes de RabbitMQ, valida reglas de negocio, ejecuta transacciones en la DB y publica notificaciones, cualquier cambio en cualquiera de esas capas obliga a abrir y arriesgar ese mismo archivo.

**Cómo cumple nuestra implementación:**
- [ProcessApprovedPaymentCommandHandler](paymentService/src/MsPaymentService.Application/UseCases/ProcessApprovedPayment/ProcessApprovedPaymentCommandHandler.cs) — solo orquesta: valida ticket, verifica TTL, delega transición al puerto `ITicketStateService`.
- [TicketStateService](paymentService/src/MsPaymentService.Infrastructure/Services/TicketStateService.cs) — solo ejecuta transacciones de estado en la DB con EF Core.
- [PaymentApprovedEventHandler](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentApprovedEventHandler.cs) — solo deserializa el JSON del evento RabbitMQ y construye el Command.
- [PaymentEventDispatcherImpl](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentEventDispatcherImpl.cs) — solo enruta por nombre de cola al handler correspondiente.

#### O: Open/Closed Principle (Abierto/Cerrado)
**Definición:** "Las entidades deben estar abiertas para extensión, cerradas para modificación".

**Ejemplo clave:**
Si agregar soporte para un nuevo tipo de evento (e.g., `ticket.payment.refunded`) obliga a editar el consumer o el dispatcher existente, esas clases no están cerradas.

**Cómo cumple nuestra implementación:**
- Agregar un nuevo tipo de evento implica implementar `IPaymentEventHandler` con su propio `QueueName`, sin modificar [PaymentEventDispatcherImpl](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentEventDispatcherImpl.cs) ni ningún handler existente.
- El dispatcher registra todos los `IPaymentEventHandler` vía inyección de colección (`IEnumerable<IPaymentEventHandler>`); la extensión es transparente.

#### L: Liskov Substitution Principle (Sustitución de Liskov)
**Definición:** "Los subtipos deben ser sustituibles por su tipo base sin romper el programa".

**Ejemplo clave:**
Si `ITicketStateService.TransitionToPaidAsync` promete retornar `true` al éxito y `false` al fallo, una implementación que lanza excepción no manejada en lugar de retornar `false` rompe el handler que depende de ese contrato.

**Cómo cumple nuestra implementación:**
- [TicketStateService](paymentService/src/MsPaymentService.Infrastructure/Services/TicketStateService.cs) respeta el contrato de `ITicketStateService`: retorna `bool` para transiciones, no lanza excepciones no documentadas.
- En tests, `NSubstitute` reemplaza `ITicketStateService` con un sustituto que retorna `true`/`false` según el escenario, y los command handlers se comportan idénticamente a producción.

#### I: Interface Segregation Principle (Segregación de Interfaces)
**Definición:** "Ningún cliente debe depender de métodos que no usa".

**Ejemplo clave:**
Una interfaz única `IDataRepository` con operaciones de tickets, pagos e historial obliga a `ProcessApprovedPaymentCommandHandler` a depender de métodos de historial que no invoca directamente.

**Cómo cumple nuestra implementación:**
- [ITicketRepository](paymentService/src/MsPaymentService.Domain/Interfaces/ITicketRepository.cs) expone únicamente operaciones de lectura/actualización de tickets necesarias para procesamiento de pagos (`GetByIdAsync`, `GetByIdForUpdateAsync`, `UpdateAsync`, `GetExpiredReservedTicketsAsync`).
- [IPaymentRepository](paymentService/src/MsPaymentService.Domain/Interfaces/IPaymentRepository.cs) expone exclusivamente operaciones de pagos (`GetByTicketIdAsync`, `GetByIdAsync`, `UpdateAsync`, `CreateAsync`).
- [ITicketStateService](paymentService/src/MsPaymentService.Application/Interfaces/ITicketStateService.cs) agrupa las tres transiciones de estado que consume Application, sin filtrar a los command handlers métodos de infraestructura irrelevantes.

#### D: Dependency Inversion Principle (Inversión de Dependencias)
**Definición:** "Los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones".

**Ejemplo clave:**
Si `ProcessApprovedPaymentCommandHandler` importara `PaymentDbContext` para ejecutar la transacción directamente, la lógica de negocio quedaría acoplada a EF Core y sería imposible testearla sin una DB real.

**Cómo cumple nuestra implementación:**
- `ProcessApprovedPaymentCommandHandler` depende de `ITicketRepository`, `IPaymentRepository` e `ITicketStateService` (abstracciones definidas en Domain y Application).
- `TicketStateService` en Infrastructure implementa `ITicketStateService`, invirtiendo la dirección: la política de negocio define el contrato y la infraestructura se adapta.

### 4.3 Patrones de diseño aplicados (definición + ejemplo + aplicación real)

#### Patrón arquitectónico: Ports & Adapters (Hexagonal)
**Definición:** El dominio y los casos de uso son el núcleo aislado; toda integración técnica (mensajería, DB) ocurre a través de puertos.

**Ejemplo clave:**
Si `ProcessApprovedPaymentCommandHandler` conociera exchanges, routing keys o el `IChannel` de RabbitMQ, cualquier migración a Kafka obligaría a reescribir lógica de negocio.

**Cómo cumple nuestra implementación:**
- Los command handlers de Application no conocen RabbitMQ, EF Core ni PostgreSQL.
- Infrastructure provee los adaptadores: [TicketPaymentConsumer](paymentService/src/MsPaymentService.Infrastructure/Messaging/RabbitMQ/TicketPaymentConsumer.cs) (adaptador de entrada), repositorios EF Core (adaptadores de salida), [TicketStateService](paymentService/src/MsPaymentService.Infrastructure/Services/TicketStateService.cs) (adaptador de transacciones).

#### Patrón creacional: Dependency Injection (Composition Root)
**Definición:** La construcción y ensamblaje de todas las dependencias se centraliza en un único punto fuera de la lógica de negocio.

**Ejemplo clave:**
Sin DI, `TicketPaymentConsumer` instanciaría `ProcessApprovedPaymentCommandHandler` con `new`, acoplando consumer y casos de uso.

**Cómo cumple nuestra implementación:**
- Todo el registro está centralizado en [paymentService/src/MsPaymentService.Infrastructure/DependencyInjection.cs](paymentService/src/MsPaymentService.Infrastructure/DependencyInjection.cs).
- Los command handlers, repositorios, `TicketStateService` y la colección de `IPaymentEventHandler` se inyectan por constructor.

#### Patrón estructural: Repository
**Definición:** Se encapsula el acceso a persistencia detrás de una abstracción orientada al dominio.

**Ejemplo clave:**
Sin Repository, las queries con `FOR UPDATE` (bloqueo pesimista) y la lógica de concurrencia optimista con `version` estarían dispersas en los command handlers.

**Cómo cumple nuestra implementación:**
- `ITicketRepository` y `IPaymentRepository` definen contratos orientados al dominio.
- Las implementaciones en [Infrastructure/Persistence/Repositories/](paymentService/src/MsPaymentService.Infrastructure/Persistence/Repositories/) encapsulan raw SQL con `FromSqlRaw` (bloqueo pesimista), optimistic locking y mapeo EF Core.

#### Patrón de comportamiento: Strategy + Dispatcher
**Definición:** Un dispatcher delega a uno de varios handlers intercambiables, seleccionado en tiempo de ejecución según un criterio (la cola de origen); cada handler es una estrategia independiente.

**Ejemplo clave:**
Un `switch` en el consumer que evalúa el nombre de la cola y llama código inline por caso es frágil: agregar un nuevo tipo de evento implica modificar el consumer.

**Cómo cumple nuestra implementación:**
- [IPaymentEventHandler](paymentService/src/MsPaymentService.Infrastructure/Handlers/IPaymentEventHandler.cs) define el contrato (`QueueName` + `HandleAsync`).
- [PaymentApprovedEventHandler](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentApprovedEventHandler.cs), [PaymentRejectedEventHandler](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentRejectedEventHandler.cs) y [PaymentRequestedEventHandler](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentRequestedEventHandler.cs) son estrategias intercambiables.
- [PaymentEventDispatcherImpl](paymentService/src/MsPaymentService.Infrastructure/Handlers/PaymentEventDispatcherImpl.cs) selecciona la estrategia por `QueueName.EndsWith(...)`, sin conocer los detalles de cada handler.

#### Patrón de comportamiento: Command Handler
**Definición:** Cada comando encapsula una intención de negocio única y su flujo de ejecución.

**Ejemplo clave:**
Mezclar aprobación y rechazo de pagos en un solo método con condicional por tipo aumenta la complejidad ciclomática y el riesgo de regresiones.

**Cómo cumple nuestra implementación:**
- [ProcessApprovedPaymentCommandHandler](paymentService/src/MsPaymentService.Application/UseCases/ProcessApprovedPayment/ProcessApprovedPaymentCommandHandler.cs) encapsula el flujo completo de aprobación: validación de ticket, verificación de TTL, creación de pago, transición a `paid`.
- [ProcessRejectedPaymentCommandHandler](paymentService/src/MsPaymentService.Application/UseCases/ProcessRejectedPayment/ProcessRejectedPaymentCommandHandler.cs) encapsula el flujo de rechazo: validación de ticket, transición a `released`.
- Cada handler es independiente y testeable de forma aislada.

### 4.4 Tests unitarios puros y cobertura lógica

Suite principal de aplicación:

- [paymentService/tests/MsPaymentService.Application.Tests/ProcessApprovedPaymentCommandHandlerTests.cs](paymentService/tests/MsPaymentService.Application.Tests/ProcessApprovedPaymentCommandHandlerTests.cs)
- [paymentService/tests/MsPaymentService.Application.Tests/ProcessRejectedPaymentCommandHandlerTests.cs](paymentService/tests/MsPaymentService.Application.Tests/ProcessRejectedPaymentCommandHandlerTests.cs)
- [paymentService/tests/MsPaymentService.Application.Tests/PaymentApprovedEventHandlerTests.cs](paymentService/tests/MsPaymentService.Application.Tests/PaymentApprovedEventHandlerTests.cs)
- [paymentService/tests/MsPaymentService.Application.Tests/PaymentRejectedEventHandlerTests.cs](paymentService/tests/MsPaymentService.Application.Tests/PaymentRejectedEventHandlerTests.cs)
- [paymentService/tests/MsPaymentService.Application.Tests/PaymentEventDispatcherImplTests.cs](paymentService/tests/MsPaymentService.Application.Tests/PaymentEventDispatcherImplTests.cs)

Evidencia de pureza unitaria:

- NSubstitute sobre `ITicketRepository`, `IPaymentRepository`, `ITicketStateService`.
- Sin DB real, sin RabbitMQ real, sin Docker para la lógica de Application.

Cobertura lógica por caso de uso:

- `ProcessApprovedPayment`: ticket no encontrado; ticket ya pagado (idempotencia); estado inválido; TTL vencido (libera ticket); pago no existe (se crea); transición exitosa a `paid`; fallo de transición.
- `ProcessRejectedPayment`: ticket no encontrado; transición exitosa a `released`; fallo de transición.
- `PaymentApprovedEventHandler` / `PaymentRejectedEventHandler`: deserialización correcta de JSON; comando construido con los campos esperados; error de deserialización retorna `Failure`.
- `PaymentEventDispatcherImpl`: enruta correctamente por nombre de cola; retorna `null` para cola desconocida; múltiples handlers registrados — selecciona el correcto.

---

## 5) Límites de evidencia y brechas abiertas

Para evitar sobreafirmaciones, estos puntos quedan explícitos:

- La separación por capas y la dirección de dependencias sí están sustentadas por referencias entre proyectos.
- La evidencia de SOLID es desigual: SRP, DIP e ISP están mejor respaldados que OCP y LSP.
- OCP y LSP se justifican por diseño y contratos declarados, pero no se demuestran con pruebas específicas de sustitución/extensión.
- Existen `HUMAN CHECK` en puntos críticos identificados, pero no hay inventario formal de "todo código crítico" del servicio.
- El repositorio aún no define un umbral formal obligatorio de coverage en CI/CD para estos servicios.

### 5.1 Evidencia ejecutada

Ejecuciones con `dotnet test --collect:"XPlat Code Coverage" -v minimal`:

- **ReservationService** (2026-02-19):
  - Comando: `dotnet test ReservationService/tests/ReservationService.Application.Tests/ReservationService.Application.Tests.csproj --collect:"XPlat Code Coverage" -v minimal`
  - Resultado: `Passed: 4, Failed: 0`
  - Cobertura (Cobertura XML):
    - `Line rate total Application: 78.33% (47/60)`
    - `Branch rate total Application: 100.00%`
    - `Line rate en UseCases (reglas de negocio): 95.65% (44/46)`

- **Producer** (2026-02-19):
  - Comando: `dotnet test producer/tests/Producer.Application.Tests/Producer.Application.Tests.csproj --collect:"XPlat Code Coverage" -v minimal`
  - Resultado: `Passed: 3, Failed: 0`
  - Cobertura (Cobertura XML):
    - `Line rate total Application: 100.00% (66/66)`
    - `Branch rate total Application: 100.00%`
    - `Line rate en UseCases (reglas de negocio): 100.00% (52/52)`

- **CrudService** (2026-02-20):
  - Comando: `dotnet test crud_service/CrudService.sln --collect:"XPlat Code Coverage" -v minimal`
  - Resultado: `Passed: 19, Failed: 0`
  - Cobertura (Cobertura XML):
    - `Line rate total: 90.97%`
    - `Branch rate total: 96.87%`
    - `Line rate Application (casos de uso): 93.82%`
    - `Branch rate Application (casos de uso): 96.87%`

- **PaymentService** (2026-02-20):
  - Comando: `dotnet test paymentService/MsPaymentService.sln --collect:"XPlat Code Coverage" -v minimal`
  - Resultado: `Passed: 25, Failed: 0`
  - Cobertura (Cobertura XML):
    - `Line rate Application: 84.21%`
    - `Branch rate Application: 100.00%`
    - `Line rate UseCases — ProcessApprovedPaymentCommandHandler: 91%`
    - `Line rate UseCases — ProcessRejectedPaymentCommandHandler: 85%`

### 5.2 Cobertura: porcentaje requerido vs estado actual

Situación actual (verificada):

- El repositorio no define un umbral formal obligatorio de cobertura en CI/CD (no hay workflow/regla de threshold versionado).
- Los cuatro servicios reportan coverage porcentual con `XPlat Code Coverage`.
- El indicador más relevante es la cobertura en `Application/UseCases` (reglas de negocio):
  - ReservationService: `95.65%`
  - Producer: `100.00%`
  - CrudService: `93.82%`
  - PaymentService: `84.21%`

Criterio recomendado (explícito, no aún enforceado):

- **Umbral mínimo sugerido para Application:** `>= 80%` line coverage.
- **Meta saludable para casos de uso críticos:** `>= 90%` line coverage.

Brecha pendiente para cerrar cumplimiento formal:

- Definir y versionar una regla de threshold (pipeline o configuración de test) para que el porcentaje no dependa de validación manual.

## Conclusión técnica

Con la evidencia actual, para los cuatro microservicios (`ReservationService`, `Producer`, `CrudService`, `PaymentService`) se puede afirmar con precisión:

- Arquitectura hexagonal aplicada con separación efectiva por capas en los cuatro servicios.
- Dependencias dirigidas correctamente (Domain ← Application ← Infrastructure ← Worker/Api) y puertos explícitos en todos los casos.
- Patrones de diseño documentados y trazables a código concreto.
- Tests unitarios de Application orientados a lógica de negocio con mocks de puertos: 4 + 3 + 19 + 25 = **51 tests unitarios pasando sin Docker ni DB**.
- Cobertura en `Application/UseCases` por encima del umbral mínimo sugerido (80%) en los cuatro servicios.

Y no se debe afirmar como completamente demostrado (aún):

- "Cumplimiento total de SOLID" sin matizar el nivel de evidencia por principio (OCP y LSP son demostrados por diseño y contratos, no por pruebas específicas de sustitución/extensión).
- "Cobertura de todo código crítico" mientras no exista threshold formal enforceado en CI/CD para los cuatro servicios.
