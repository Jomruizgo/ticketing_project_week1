# Reporte de Deuda Técnica — TicketRush

**Fecha:** 2026-02-16
**Autores:** JR + EM
**Metodología:** Mock Imposible — intentar escribir tests unitarios puros (sin Docker, sin DB, sin RabbitMQ) para cada clase con lógica de negocio. Cada fricción detectada se documenta como señal de baja calidad.

---

## 1. Smells Detectados

### 1.1 PaymentService (JR)

#### MOCK IMPOSIBLE — Clases no testeables sin infraestructura

| # | Archivo | Líneas | Smell | Principio SOLID Violado | Descripción |
|---|---------|--------|-------|------------------------|-------------|
| 1 | `paymentService/.../Services/TicketStateService.cs` | L10, L17 | Dependencia concreta de `PaymentDbContext` | **DIP** | Constructor exige `PaymentDbContext` concreto en vez de abstracción. |
| 2 | `paymentService/.../Services/TicketStateService.cs` | L32, L106 | Transacciones DB en lógica de negocio | **SRP** | Llama `_dbContext.Database.BeginTransactionAsync()` directamente. Mezcla gestión transaccional + lógica de estados + historial en un solo método. |
| 3 | `paymentService/.../Repositories/TicketRepository.cs` | L36 | SQL crudo no mockeable | **DIP** | `FromSqlRaw("SELECT ... FOR UPDATE")` — SQL específico de PostgreSQL imposible de mockear sin una DB real. |
| 4 | `paymentService/.../Repositories/TicketRepository.cs` | L46 | SQL crudo para UPDATE | **DIP** | `ExecuteSqlRawAsync` con SQL crudo para actualización con concurrencia optimista. Acoplado a dialecto PostgreSQL. |
| 5 | `paymentService/.../Messaging/TicketPaymentConsumer.cs` | L19 | Dependencia concreta de `RabbitMQConnection` | **DIP** | Depende de la clase concreta `RabbitMQConnection`, no de una interfaz. |
| 6 | `paymentService/.../Messaging/RabbitMQConnection.cs` | L7 | Clase sin interfaz | **DIP** | No expone interfaz. Crea `ConnectionFactory` internamente (L44). Imposible sustituir en tests. |

#### Otros smells

| # | Archivo | Líneas | Smell | Tipo | Descripción |
|---|---------|--------|-------|------|-------------|
| 7 | `paymentService/.../Services/PaymentValidationService.cs` | L143 | Magic Number | Code Smell | `AddMinutes(5)` hardcodeado. Existe `PaymentSettings.ReservationTtlMinutes` pero no se inyecta. |
| 8 | `paymentService/.../Configurations/RabbitMQSettings.cs` | L10-14 | Lectura de env vars en propiedades | Code Smell | `Environment.GetEnvironmentVariable()` en defaults de propiedades. Mezcla configuración con lectura de entorno. |

#### Clases testeables (tests escritos)

| Clase | Tests | Resultado |
|-------|-------|-----------|
| `PaymentValidationService` | 15 | ✅ 15/15 |
| `PaymentApprovedEventHandler` | 4 | ✅ 4/4 |
| `PaymentRejectedEventHandler` | 3 | ✅ 3/3 |
| `PaymentEventDispatcherImpl` | 3 | ✅ 3/3 |

### 1.2 CrudService (EM)

#### Smells estructurales

| # | Archivo | Líneas | Smell | Principio SOLID Violado | Descripción |
|---|---------|--------|-------|------------------------|-------------|
| 13 | `crud_service/Data/RepositoriesImplementation.cs` | L1-201 | God File — 4 repos en un solo archivo | **SRP** | `EventRepository`, `TicketRepository`, `PaymentRepository` y `TicketHistoryRepository` viven en el mismo archivo. Dificulta navegación y ownership. |
| 14 | `crud_service/Repositories/IRepositories.cs` | L1-52 | Múltiples interfaces en un archivo | **ISP / Organización** | 4 interfaces (`IEventRepository`, `ITicketRepository`, `IPaymentRepository`, `ITicketHistoryRepository`) en un solo archivo. Menor gravedad, pero rompe convención 1-tipo-por-archivo. |
| 15 | `crud_service/Data/RepositoriesImplementation.cs` | L12, L68, L130, L175 | Dependencia concreta de `TicketingDbContext` | **DIP** | Los 4 repos dependen del `TicketingDbContext` concreto en vez de abstraerlo. No es posible hacer un test unitario del repo sin un DbContext real o InMemory. |

#### Clases testeables (tests escritos)

| Clase | Tests | Resultado |
|-------|-------|-----------|
| `EventService` | 8 | ✅ 8/8 |
| `TicketService` | 11 | ✅ 11/11 |

#### Conclusiones de los tests

- **`EventService` y `TicketService` son 100% testeables sin infraestructura.** Gracias a que dependen de interfaces (`IEventRepository`, `ITicketRepository`, `ITicketHistoryRepository`), se pueden mockear completamente con NSubstitute.
- **Los repositorios NO son testeables sin DB.** Dependen del `TicketingDbContext` concreto. Para testearlos se necesitaría `InMemoryDatabase` o un contenedor PostgreSQL, lo cual viola la premisa "Mock Imposible".
- **El mapeo DTO funciona correctamente:** los tests verificaron conteo de tickets por estado (Available/Reserved/Paid), actualización parcial de campos, y persistencia de historial.
- **No hay acoplamiento a messaging:** a diferencia de PaymentService y ReservationService, CrudService no tiene dependencia de RabbitMQ. Su deuda es puramente organizacional (God File, múltiples tipos por archivo).

### 1.3 ReservationService (EM)

#### MOCK IMPOSIBLE — Consumer no testeable sin RabbitMQ

| # | Archivo | Líneas | Smell | Principio SOLID Violado | Descripción |
|---|---------|--------|-------|------------------------|-------------|
| 16 | `ReservationService/.../Consumers/TicketReservationConsumer.cs` | L36-42 | `ConnectionFactory` instanciada internamente | **DIP** | Crea `new ConnectionFactory { HostName, Port, UserName, Password }` directamente. No se puede sustituir por un mock. |
| 17 | `ReservationService/.../Consumers/TicketReservationConsumer.cs` | L19-20, L44-45 | Dependencia concreta de `IConnection` e `IChannel` (RabbitMQ) | **DIP** | Campos tipados con tipos concretos de RabbitMQ (`IConnection`, `IChannel`). Acoplado al protocolo AMQP. |
| 18 | `ReservationService/.../Consumers/TicketReservationConsumer.cs` | L50, L53-86 | Consumer, deserialización y ACK en un solo método | **SRP** | `ExecuteAsync` maneja conexión, suscripción, deserialización JSON, resolución de servicios via scope, procesamiento y ACK. 5 responsabilidades en un método de 60 líneas. |
| 19 | `ReservationService/.../Consumers/TicketReservationConsumer.cs` | L84 | ACK en caso de error (sin retry) | Code Smell | Hace `BasicAckAsync` incluso cuando falla el procesamiento. El mensaje se pierde sin posibilidad de reintento. |

#### Clases testeables

| Clase | Tests | Resultado |
|-------|-------|-----------|
| `ReservationServiceImpl` | 4 (preexistentes) | ✅ 4/4 |

#### Conclusiones de los tests

- **`ReservationServiceImpl` es testeable sin infraestructura.** Depende de `ITicketRepository` (abstracción), por lo que se puede mockear completamente.
- **`TicketReservationConsumer` es IMPOSIBLE de testear sin RabbitMQ.** Instancia `ConnectionFactory` internamente y depende de tipos concretos de RabbitMQ (`AsyncEventingBasicConsumer`, `BasicDeliverEventArgs`, `IChannel`). Es el caso más claro de "Mock Imposible" en este microservicio.

### 1.4 Producer (JR)

| # | Archivo | Líneas | Smell | Tipo | Descripción |
|---|---------|--------|-------|------|-------------|
| 9 | `producer/.../Controllers/PaymentsController.cs` | L151-158 | Lógica de negocio en Controller | **SRP** | `SimulatePaymentProcessing` con `Random.Shared` vive en el controlador. No testeable, no sustituible. |
| 10 | `producer/.../Controllers/PaymentsController.cs` | L157 | Simulación no determinista | Testeabilidad | `Random.Shared.Next(0, 100) < 80` — comportamiento no predecible en tests. |
| 11 | `producer/.../Services/RabbitMQPaymentPublisher.cs` | L40-93 | Código duplicado | **DRY** | Patrón idéntico de serialize + properties + publish repetido en `PublishPaymentApprovedAsync` y `PublishPaymentRejectedAsync`. |
| 12 | `producer/.../Services/RabbitMQTicketPublisher.cs` | L36-54 | Código duplicado (cross-class) | **DRY** | Mismo patrón serialize + publish que en `RabbitMQPaymentPublisher`. 3 métodos con estructura idéntica. |

---

## 2. Clasificación de Deuda Técnica

### Por tipo (completo — todos los servicios)

| Tipo de Deuda | Ocurrencias | Smells # |
|---------------|-------------|----------|
| **Acoplamiento a infraestructura (DIP)** | 7 | 1, 3, 4, 5, 6, 15, 16, 17 |
| **Violación SRP** | 3 | 2, 9, 18 |
| **Organización / God File** | 2 | 13, 14 |
| **Magic Numbers / Config hardcodeada** | 2 | 7, 8 |
| **Código duplicado (DRY)** | 2 | 11, 12 |
| **No determinismo en lógica** | 1 | 10 |
| **Pérdida silenciosa de mensajes** | 1 | 19 |

---

## 3. Métricas (parcial)

| Métrica | Valor |
|---------|-------|
| Tests unitarios PaymentService | 25/25 ✅ |
| Tests unitarios CrudService | 19/19 ✅ (8 EventService + 11 TicketService) |
| Tests unitarios ReservationService | 4/4 ✅ (preexistentes) |
| **Total tests unitarios** | **48/48 ✅** |

---

## 4. Test del CTO

> **"Si mañana cambiamos RabbitMQ por Kafka o AWS SQS, ¿hay que reescribir lógica de negocio?"**

### PaymentService (JR)

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `PaymentValidationService` | **NO** | Pura lógica de validación. |
| `PaymentApprovedEventHandler` | **Parcialmente** | Deserialización JSON agnóstica. Requiere re-wireado de DI, no reescritura. |
| `PaymentRejectedEventHandler` | **Parcialmente** | Mismo caso. |
| `PaymentEventDispatcherImpl` | **Mínimo** | Usa `QueueName` como concepto de routing. Con Kafka serían topics pero el dispatcher funciona igual. |
| `TicketStateService` | **NO** | No depende de messaging. Solo de DB. |
| `TicketPaymentConsumer` | **SI — REESCRITURA** | Acoplado a `RabbitMQConnection`, `IModel`, `BasicDeliverEventArgs`. |
| `RabbitMQConnection` | **SI — REEMPLAZO** | Se elimina y se crea equivalente Kafka. |

### Producer (JR)

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `RabbitMQPaymentPublisher` | **SI — REEMPLAZO** | Se crea `KafkaPaymentPublisher`. La interfaz `IPaymentPublisher` se mantiene. |
| `RabbitMQTicketPublisher` | **SI — REEMPLAZO** | Se crea `KafkaTicketPublisher`. La interfaz `ITicketPublisher` se mantiene. |
| `PaymentsController` | **NO** | Depende de `IPaymentPublisher` (interfaz). |

### CrudService (EM)

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `EventService` | **NO** | Pura lógica de negocio. No depende de messaging. |
| `TicketService` | **NO** | Pura lógica de negocio. No depende de messaging. |
| `EventRepository` | **NO** | Solo depende de DB (EF Core). |
| `TicketRepository` | **NO** | Solo depende de DB (EF Core). |

**CrudService no tiene dependencia de RabbitMQ.** Migrar de broker no requiere tocar ningún archivo.

### ReservationService (EM)

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `ReservationServiceImpl` | **NO** | Pura lógica de negocio. Depende solo de `ITicketRepository`. |
| `TicketRepository` | **NO** | Solo depende de DB. |
| `TicketReservationConsumer` | **SI — REESCRITURA** | Acoplado a `ConnectionFactory`, `IChannel`, `AsyncEventingBasicConsumer`, `BasicAckAsync`. Todo el `ExecuteAsync` (L33-96) es RabbitMQ-específico. |
| `RabbitMQSettings` | **SI — REEMPLAZO** | Se elimina y se crea `KafkaSettings` equivalente. |

---

## 5. Priorización de Resolución

> Ver `docs/REFACTORING_PLAN.md` para el backlog completo (P0/P1/P2).
> La Fase 2 está bloqueada hasta definir la arquitectura hexagonal.
