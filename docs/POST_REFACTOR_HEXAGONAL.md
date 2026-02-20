# Post-refactorización: validación técnica (ReservationService y Producer)

## Alcance

Este documento evidencia la calidad de la refactorización en los dos microservicios migrados a arquitectura hexagonal:

- ReservationService
- Producer

No cubre aún `paymentService` ni `crud_service` como arquitectura objetivo, porque siguen fuera del alcance de migración total.

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

- Puerto de entrada de aplicación: [ReservationService/src/ReservationService.Application/Interfaces/IProcessReservationUseCase.cs](ReservationService/src/ReservationService.Application/Interfaces/IProcessReservationUseCase.cs)
- Puerto de salida de dominio: [ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs](ReservationService/src/ReservationService.Domain/Interfaces/ITicketRepository.cs)
- Caso de uso desacoplado de infraestructura: [ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs](ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs)

DTOs de Application definidos al mismo nivel de Interfaces/UseCases:

- [ReservationService/src/ReservationService.Application/DTOs/ProcessReservation/ProcessReservationCommand.cs](ReservationService/src/ReservationService.Application/DTOs/ProcessReservation/ProcessReservationCommand.cs)
- [ReservationService/src/ReservationService.Application/DTOs/ProcessReservation/ProcessReservationResponse.cs](ReservationService/src/ReservationService.Application/DTOs/ProcessReservation/ProcessReservationResponse.cs)

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

### 1.3 Patrones de diseño GoF aplicados (definición + ejemplo + aplicación real)

#### Patrón estructural: Adapter
**Definición:** Adapter traduce una interfaz/protocolo externo hacia una interfaz útil para el núcleo de negocio.

**Ejemplo clave:**
RabbitMQ entrega bytes y metadatos de transporte, pero el caso de uso necesita un comando tipado y una invocación de puerto de entrada.

**Cómo cumple nuestra implementación:**
- [ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs](ReservationService/src/ReservationService.Infrastructure/Messaging/RabbitMQConsumer.cs) adapta el mensaje RabbitMQ a `ProcessReservationCommand` y delega al puerto `IProcessReservationUseCase`.

#### Patrón de comportamiento: Command
**Definición:** Command encapsula una solicitud como objeto para separar quien invoca de quien ejecuta.

**Ejemplo clave:**
La reserva se encapsula en `ProcessReservationCommand`; el invocador (consumer) no conoce la lógica interna de ejecución.

**Cómo cumple nuestra implementación:**
- Comando: [ReservationService/src/ReservationService.Application/DTOs/ProcessReservation/ProcessReservationCommand.cs](ReservationService/src/ReservationService.Application/DTOs/ProcessReservation/ProcessReservationCommand.cs)
- Ejecutor del comando: [ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs](ReservationService/src/ReservationService.Application/UseCases/ProcessReservation/ProcessReservationCommandHandler.cs)

No se evidencia un patrón creacional GoF explícito en el flujo actual de ReservationService.

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
- Dependencia de infraestructura hacia puerto de entrada (y no handler concreto) en [ReservationService/src/ReservationService.Infrastructure/DependencyInjection.cs](ReservationService/src/ReservationService.Infrastructure/DependencyInjection.cs).

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

Puertos de entrada de aplicación definidos:

- [producer/src/Producer.Application/Interfaces/IReserveTicketUseCase.cs](producer/src/Producer.Application/Interfaces/IReserveTicketUseCase.cs)
- [producer/src/Producer.Application/Interfaces/IRequestPaymentUseCase.cs](producer/src/Producer.Application/Interfaces/IRequestPaymentUseCase.cs)

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
- Controllers consumen puertos de entrada de Application (`IReserveTicketUseCase`, `IRequestPaymentUseCase`) y no handlers concretos.
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
- `Producer.Api` depende de puertos de entrada en `Producer.Application/Interfaces`, no de implementaciones concretas de casos de uso.
- `Producer.Infrastructure` contiene RabbitMQ, DI y configuración concreta.

### 2.3 Patrones de diseño GoF aplicados (definición + ejemplo + aplicación real)

#### Patrón creacional: Factory Method
**Definición:** Factory Method encapsula la creación de un objeto complejo detrás de una fábrica concreta.

**Ejemplo clave:**
Si cada publisher creara conexión a RabbitMQ manualmente con parámetros hardcodeados, habría duplicación y alto riesgo de inconsistencia.

**Cómo cumple nuestra implementación:**
- `ConnectionFactory` de RabbitMQ centraliza la creación de conexiones/canales según configuración.
- El wiring y ciclo de vida de esas dependencias se centraliza en [producer/src/Producer.Infrastructure/DependencyInjection.cs](producer/src/Producer.Infrastructure/DependencyInjection.cs).

#### Patrón estructural: Adapter
**Definición:** Adapter traduce una interfaz esperada por negocio a una API externa concreta.

**Ejemplo clave:**
Application necesita "publicar evento"; RabbitMQ exige exchange/routing key/body/propiedades. El adapter convierte entre ambos mundos.

**Cómo cumple nuestra implementación:**
- [producer/src/Producer.Infrastructure/Messaging/RabbitMQTicketPublisher.cs](producer/src/Producer.Infrastructure/Messaging/RabbitMQTicketPublisher.cs) adapta `ITicketEventPublisher`.
- [producer/src/Producer.Infrastructure/Messaging/RabbitMQPaymentPublisher.cs](producer/src/Producer.Infrastructure/Messaging/RabbitMQPaymentPublisher.cs) adapta `IPaymentEventPublisher`.

#### Patrón de comportamiento: Command
**Definición:** Command encapsula una solicitud como objeto para separar invocador y ejecutor.

**Ejemplo clave:**
Controllers crean comandos (`ReserveTicketCommand`, `RequestPaymentCommand`) y delegan su ejecución a los casos de uso.

**Cómo cumple nuestra implementación:**
- Comandos: [producer/src/Producer.Application/DTOs/ReserveTicket/ReserveTicketCommand.cs](producer/src/Producer.Application/DTOs/ReserveTicket/ReserveTicketCommand.cs), [producer/src/Producer.Application/DTOs/RequestPayment/RequestPaymentCommand.cs](producer/src/Producer.Application/DTOs/RequestPayment/RequestPaymentCommand.cs)
- Ejecutores: [producer/src/Producer.Application/UseCases/ReserveTicket/ReserveTicketCommandHandler.cs](producer/src/Producer.Application/UseCases/ReserveTicket/ReserveTicketCommandHandler.cs), [producer/src/Producer.Application/UseCases/RequestPayment/RequestPaymentCommandHandler.cs](producer/src/Producer.Application/UseCases/RequestPayment/RequestPaymentCommandHandler.cs)

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
- Registro explícito de puertos de entrada para evitar acoplamiento de controllers a handlers concretos en [producer/src/Producer.Infrastructure/DependencyInjection.cs](producer/src/Producer.Infrastructure/DependencyInjection.cs).

---

## 3) Límites de evidencia y brechas abiertas

Para evitar sobreafirmaciones, estos puntos quedan explícitos:

- La separación por capas y la dirección de dependencias sí están sustentadas por referencias entre proyectos.
- La evidencia de SOLID es desigual: SRP, DIP e ISP están mejor respaldados que OCP y LSP.
- OCP y LSP se justifican por diseño y contratos declarados, pero no se demuestran con pruebas específicas de sustitución/extensión.
- Existen `HUMAN CHECK` en puntos críticos identificados, pero no hay inventario formal de "todo código crítico" del servicio.
- El repositorio aún no define un umbral formal obligatorio de coverage en CI/CD para estos servicios.

Brechas cerradas durante la última iteración (2026-02-20):

- Se eliminó acoplamiento de adapters de entrada a handlers concretos en ReservationService y Producer (ahora usan puertos de entrada).
- Se eliminaron abstracciones/código huérfano detectado en ReservationService (`IMessageConsumer`, `TicketNotAvailableException`).

### 3.1 Evidencia ejecutada (2026-02-19)

Ejecuciones realizadas en contenedor `mcr.microsoft.com/dotnet/sdk:8.0` (porque no hay `dotnet` instalado en host local):

- ReservationService (Application tests):
  - Comando: `dotnet test ReservationService/tests/ReservationService.Application.Tests/ReservationService.Application.Tests.csproj --collect:"XPlat Code Coverage" -v minimal`
  - Resultado: `Passed: 4, Failed: 0`
  - Cobertura (Cobertura XML):
    - `Line rate total Application: 78.33% (47/60)`
    - `Branch rate total Application: 100.00%`
    - `Line rate en UseCases (reglas de negocio): 95.65% (44/46)`
- Producer (Application tests + cobertura):
  - Comando: `dotnet test producer/tests/Producer.Application.Tests/Producer.Application.Tests.csproj --collect:"XPlat Code Coverage" -v minimal`
  - Resultado tests: `Passed: 3, Failed: 0`
  - Cobertura (Cobertura XML):
    - `Line rate total Application: 100.00% (66/66)`
    - `Branch rate total Application: 100.00%`
    - `Line rate en UseCases (reglas de negocio): 100.00% (52/52)`

### 3.2 Cobertura: porcentaje requerido vs estado actual

Situación actual (verificada):

- El repositorio no define hoy un umbral formal obligatorio de cobertura en CI/CD para estos servicios (no hay workflow/regla de threshold versionado en este alcance).
- Producer y ReservationService ya pueden reportar coverage porcentual en tests de Application (`XPlat Code Coverage`).
- El indicador más relevante para este refactor es la cobertura en `Application/UseCases` (reglas de negocio):
  - ReservationService: `95.65%`
  - Producer: `100.00%`

Criterio recomendado (explícito, no aún enforceado):

- **Umbral mínimo sugerido para Application:** `>= 80%` line coverage.
- **Meta saludable para casos de uso críticos:** `>= 90%` line coverage.

Brecha pendiente para cerrar cumplimiento formal:

- Definir y versionar una regla de threshold (pipeline o configuración de test) para que el porcentaje no dependa de validación manual.

## Conclusión técnica

Con la evidencia actual, para `ReservationService` y `Producer` se puede afirmar con precisión:

- Arquitectura hexagonal aplicada con separación efectiva por capas.
- Dependencias dirigidas correctamente y puertos explícitos.
- Patrones de diseño documentados y trazables a código.
- Tests unitarios de Application orientados a lógica de negocio con mocks de puertos.

Y no se debe afirmar como completamente demostrado (aún):

- "Cumplimiento total de SOLID" sin matizar el nivel de evidencia por principio.
- "Cobertura de todo código crítico con HUMAN CHECK" sin checklist trazable.
- "Cumplimiento de porcentaje mínimo de cobertura" mientras no exista threshold formal enforceado para ambos servicios.
